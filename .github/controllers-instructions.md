# הוראות ייעודיות ל-Controllers

קובץ זה מיועד לטיפול בשאלות ובשינויים הקשורים ל-Controllers בלבד.

## מטרת הקובץ
- להנחות את העבודה על בקרות ה-API, ניתובי HTTP, ולוגיקה של קריאת/כתיבת נתונים דרך Controller.
- לשמור על טוקן לשאלות רלוונטיות בלבד.

## עקרונות
- התייחס תמיד לתיקיית `Chinese_Auction/Controllers/`.
- בדוק אם קיימים Controllers כמו `GiftController`, `UserController`, `PurchaseController`, `PackageController`, `CategoryController`, `DonorController`.
- הבדל בין לוגיקה של Controller לבין לוגיקה של שירותים: Controllers צריכים רק לנתב, לא לבצע עיבוד עסקי כבד.

## התנהגות מומלצת
- אם השאלה היא על קוד שנמצא ב-Controller, טפל בה כאן.
- אם השאלה קשורה ל-Validation, Authentication, Authorization או Status Codes, זה גם בתחום ה-Controllers.
- אם יש צורך בשינוי ב-Services או ב-Repository כתוצאה מ-Controller, ציין זאת בקצרה והפנה לקבצים המתאימים.

## הפניות סטנדרטיות
- `Chinese_Auction/Controllers/` — נקודת כניסה ל-API.
- `Chinese_Auction/Dtos/` — אובייקטים המועברים לוהים
och מה-Controller.
- `Chinese_Auction/Services/` — השירותים המשמשים על ידי ה-Controllers.
