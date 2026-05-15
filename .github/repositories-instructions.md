# הוראות ייעודיות ל-Repository

קובץ זה מיועד לטיפול בשאלות ובשינויים הקשורים ל-Repository בלבד.

## מטרת הקובץ
- להבהיר כיצד לענות על שאלות בנושא שיטות גישה לנתונים, אינטרקציה עם DbContext, ומימוש ממשקי Repository.
- למנוע בזבוז טוקנים על נושאים אחרים.

## עקרונות
- התייחס תמיד לממשקים ולמבנים ב-`Chinese_Auction/Repository/`.
- בדוק אם קיימות מחלקות כמו `IGiftRepository`, `IUserRepository`, `GiftRepository`, `UserRepository` וכו'.
- הסבר כיצד `DbContext` נמצא ב-`ChineseAuctionDbContext.cs` ומיובא למחלקות ה-Repository.

## התנהגות מומלצת
- אם השאלה היא על Query או שינויים בנתונים, התרכז ב-Repository ובשכבות שמעליו.
- אם יש צורך, פרט כיצד להוסיף שיטה חדשה ל-Repository ולממש אותה ב-`Services` אם יש צורך.
- אין לערב נושאים של Controllers או Frontend בשאלות אלו, אלא אם יש תלות ישירה.

## הפניות סטנדרטיות
- `Chinese_Auction/Repository/` — מפעיל את הלוגיקה של גישה לנתונים.
- `Chinese_Auction/Data/ChineseAuctionDbContext.cs` — תצורת ה-DbContext.
- `Chinese_Auction/Models/` — ישויות הנתונים המשמשות ב-Repository.
