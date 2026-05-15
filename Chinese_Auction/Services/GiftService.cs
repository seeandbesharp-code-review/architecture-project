using AutoMapper;
using Chinese_Auction.Dto_s;
using Chinese_Auction.Models;
using Chinese_Auction.Repository;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text.Json;

namespace Chinese_Auction.Services
{
    public class GiftService : IGiftService
    {
        private readonly IGiftRepository _giftRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GiftRepository> _logger;
        private readonly IDistributedCache _cache;
        private readonly TimeSpan _giftListCacheTtl;
        private const string GiftListCacheKey = "GiftService:AllGifts";

        public GiftService(
            IGiftRepository giftRepository,
            IMapper mapper,
            ILogger<GiftRepository> logger,
            IDistributedCache cache,
            IConfiguration configuration)
        {
            _giftRepository = giftRepository;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
            var ttlSeconds = configuration.GetValue<int?>("Redis:GiftListTTLSeconds") ?? 60;
            _giftListCacheTtl = TimeSpan.FromSeconds(ttlSeconds);
        }

        public async Task<IEnumerable<GetGiftDto>> GetAllGiftsAsync()
        {
            var cachedJson = await _cache.GetStringAsync(GiftListCacheKey);
            if (!string.IsNullOrWhiteSpace(cachedJson))
            {
                _logger.LogInformation("Returning gifts from Redis cache.");
                return JsonSerializer.Deserialize<IEnumerable<GetGiftDto>>(cachedJson) ?? Enumerable.Empty<GetGiftDto>();
            }

            var gifts = await _giftRepository.GetAllGiftsAsync();
            var dto = _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = _giftListCacheTtl
            };

            await _cache.SetStringAsync(GiftListCacheKey, JsonSerializer.Serialize(dto), cacheOptions);
            _logger.LogInformation("Stored gift list in Redis cache for {TtlSeconds} seconds.", _giftListCacheTtl.TotalSeconds);
            return dto;
        }

        private Task InvalidateGiftListCacheAsync()
        {
            return _cache.RemoveAsync(GiftListCacheKey);
        }

        public async Task<GetGiftDto?> GetGiftByIdAsync(int id)
        {
            var gift = await _giftRepository.GetGiftByIdAsync(id);
            if (gift == null)
            {
                _logger.LogWarning($"Gift with ID {id} not found.");
                return null; 
            }
            return _mapper.Map<GetGiftDto?>(gift);
        }

        public async Task<IEnumerable<GetGiftDto>> GetGiftsByCategoryIdAsync(int categoryId)
        {
            var gifts = await _giftRepository.GetGiftsByCategoryIdAsync(categoryId);
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }


        public async Task<GetGiftDto> CreateGiftAsync(GiftDto gift)
        {
            var createGift = _mapper.Map<Gift>(gift);
            var addedGift = await _giftRepository.CreateGiftAsync(createGift);
            await InvalidateGiftListCacheAsync();
            return _mapper.Map<GetGiftDto>(addedGift);
        }

        public async Task<GetGiftDto?> UpdateGiftAsync(int id, GiftDto gift)
        {
            var existingGift = await _giftRepository.GetGiftByIdAsync(id);
            if (existingGift == null)
            {
                _logger.LogWarning($"Gift with ID {id} not found for update.");
                return null;
            }
            if (existingGift.Purchases.Any() || existingGift.Purchases.Count() > 0)
                throw new InvalidOperationException("לא ניתן למחוק מתנה שכבר נבחרה להגרלה");
            _mapper.Map(gift,existingGift);
            existingGift.Id = id;
            var updatedGift = await _giftRepository.UpdateGiftAsync(existingGift);
            if (updatedGift == null)
            {
                _logger.LogError($"Failed to update Gift with ID {id}.");
                return null;
            }
            await InvalidateGiftListCacheAsync();
            return _mapper.Map<GetGiftDto>(updatedGift);
        }


        public async Task<UpdateGiftDto?> UpdateGiftPurchasesQuantityAsync(int id)
        {
            var existingGift = await _giftRepository.GetGiftByIdAsync(id);
            if (existingGift == null)
            {
                _logger.LogWarning($"Gift with ID {id} not found for updating purchase quantity.");
                return null;
            }
            var updatedGift = await _giftRepository.UpdateGiftPurchasesQuantityAsync(id);
            if (updatedGift == null)
            {                
                _logger.LogError($"Failed to update purchase quantity for Gift with ID {id}.");
                return null;
            }
            await InvalidateGiftListCacheAsync();
            return _mapper.Map<UpdateGiftDto>(updatedGift);
        }


        public async Task<bool> DeleteGiftAsync(int id)
        {
            var existingGift = await _giftRepository.GetGiftByIdAsync(id);
            if (existingGift == null)
            {
               _logger.LogWarning($"Gift with ID {id} not found for deletion.");
                return false;
            }
            if (existingGift.Purchases.Any() || existingGift.Purchases.Count() > 0)
                throw new InvalidOperationException("לא ניתן למחוק מתנה שכבר נבחרה להגרלה");
            await _giftRepository.DeleteGiftAsync(id);
            await InvalidateGiftListCacheAsync();
            return true;
        }

        public async Task<IEnumerable<GetGiftDto>> GetFilteredGiftsAsync(string? giftName, string? donorName, int? minPurchases)
        {
            var gifts = await _giftRepository.GetFilteredGiftsAsync(giftName, donorName, minPurchases);
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }

        public async Task<IEnumerable<GetGiftDto>> GetSortedGiftsAsync(string sortBy)
        {
            var gifts = await _giftRepository.GetAllGiftsAsync();
            if (sortBy == "value")
                gifts = gifts.OrderByDescending(g => g.Value);
            else if (sortBy == "category")
                gifts = gifts
                    .Where(g => g.Category != null)
                    .OrderByDescending(g => g.Category!.Name);
            return _mapper.Map<IEnumerable<GetGiftDto>>(gifts);
        }





    }
}
