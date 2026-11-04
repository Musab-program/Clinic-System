# ClinicSystem — نظام إدارة العيادة

واجهة REST API لإدارة حسابات المستخدمين والمواعيد والتخصصات والخدمات والدفعات. المشروع مبني على ASP.NET Core وEntity Framework Core وSQL Server وASP.NET Core Identity وJWT.

> هذه نسخة تطويرية/تعليمية. لا تستخدم بيانات Seeder أو كلمة مرور تجريبية أو مفتاح JWT الموجود في ملف الإعدادات للإنتاج. خذ نسخة احتياطية قبل تطبيق الترحيلات على قاعدة فعلية.

## 1. مكونات المشروع

| المشروع | المسؤولية |
|---|---|
| `Clinic.Domain` | كيانات المجال والحالات والتعدادات، مثل Appointment وDoctor وPatient وPayment |
| `Clinic.Applicaation` | خدمات حالات الاستخدام وDTOs والعقود (`IApplicationDbContext`, `IAppointmentService`, `IAuthService`) |
| `Clinic.Infrastructure` | EF Core/SQL Server، Identity، إصدار JWT، الاستمرارية والترحيلات وSeeder التطويري |
| `Clinic.Application` | مشروع Web API، Controllers وتهيئة Swagger وJWT والتفويض |

ملاحظة: اسم مجلد/مشروع التطبيق `Clinic.Applicaation` مكتوب بهذا الشكل في الحل؛ لا تغيّره يدويًا دون تحديث مراجع المشاريع.

## 2. الوظائف الرئيسية وقواعد العمل

- تسجيل الدخول وإصدار JWT وRefresh Token، وتجديد/إلغاء Refresh Token. تحفظ تجزئة رمز التحديث في قاعدة البيانات لا الرمز الصريح.
- حجز المواعيد مع التحقق من وجود الطبيب والمريض، تخصص الطبيب، ساعات العمل، وقت UTC، مدة الموعد، الخدمات وكمياتها، التداخلات وسعة الطبيب اليومية.
- استخدام رسوم التخصص الأب عند غياب رسم التخصص الفرعي، وإضافة أسعار الخدمات وتثبيت الإجمالي في الموعد.
- انتقالات الحالة: `Scheduled` ثم `Confirmed` ثم `Completed`، مع إمكان الإلغاء وفق الصلاحية. لا تعدّل موعدًا مكتملًا أو ملغى.
- المريض يحجز/يلغي لمعرف ملفه فقط، ويجب أن يكون الإلغاء قبل الموعد بساعتين على الأقل. الطبيب يغيّر حالة المواعيد المسندة إليه فقط؛ الاستقبال/المدير يدير التعديل والدفع.
- الدفعات الجزئية لا تتجاوز إجمالي الرسوم. فحوص الحجز والدفع تستخدم معاملات قاعدة بيانات للمساعدة في منع التعارضات المتزامنة.

أسماء الأدوار المستخدمة في السياسات: `Admin`, `Receptionist`, `Doctor`, `Patient`. يجب إنشاء هذه الأدوار وإسنادها للمستخدمين. هذه النسخة لا تحتوي لوحة لإدارة الحسابات/الأدوار أو تسجيلًا عامًا.

## 3. المتطلبات والإعداد

- .NET 9 SDK.
- SQL Server يمكن الوصول إليه من جهاز التشغيل.
- أداة `dotnet-ef` بإصدار 9.x لإدارة الترحيلات، مثل 9.0.10:

```bash
dotnet tool install --global dotnet-ef --version 9.0.10
```

استعد وابنِ الحل من مجلد `ClinicSystem`:

```bash
dotnet restore ClinicSystem.sln
dotnet build ClinicSystem.sln
```

### سلسلة الاتصال ومفتاح JWT

الإعدادات الافتراضية في `Clinic.Application/appsettings.json` تستخدم SQL Server محليًا. عدّلها لبيئتك أو الأفضل خزّن الأسرار خارج ملفات المصدر عبر User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=ClinicDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True" --project Clinic.Application
dotnet user-secrets set "JwtSettings:SecretKey" "ضع-مفتاحًا-عشوائيًا-جديدًا-بطول 32 بايت على الأقل" --project Clinic.Application
```

على Windows PowerShell استخدم الأوامر نفسها (مع سلسلة اتصال ملائمة لـ SQL Server لديك). يجب أن يكون SecretKey نفسه عند إصدار التوكن والتحقق منه، وبقيم `Issuer` و`Audience` المطابقة لـ `JwtSettings`. لا ترفع المفتاح أو بيانات الاتصال إلى Git.

## 4. قاعدة البيانات والترحيلات

### قائمة الترحيلات

الترحيلات المضمّنة حاليًا:

1. `InitialCreate`
2. `AllowMultiplePayments`
3. `PaymentTokenAndMoneyIndexes`
4. `ChecklistCoreRules`

### عرض حالة الترحيلات

من مجلد الحل:

```bash
dotnet ef migrations list \
  --project Clinic.Infrastructure/Clinic.Infrastructure.csproj \
  --startup-project Clinic.Application/Clinic.Api.csproj
```

التطبيقات المطبقة تظهر في القائمة كـ **Applied**، وغير المطبقة كـ **Pending**. بعد التحديث يجب ألا تبقى ترحيلات معلقة.

### تطبيق كل الترحيلات المعلّقة

خذ نسخة احتياطية أولًا إذا كانت قاعدة بيانات قائمة، ثم نفّذ:

```bash
dotnet ef database update \
  --project Clinic.Infrastructure/Clinic.Infrastructure.csproj \
  --startup-project Clinic.Application/Clinic.Api.csproj
```

### تأكيد التطبيق مباشرة في SQL Server

نفّذ الاستعلام التالي على قاعدة `ClinicDb`:

```sql
SELECT MigrationId, ProductVersion
FROM dbo.__EFMigrationsHistory
ORDER BY MigrationId;
```

يجب أن تظهر السجلات الأربعة المقابلة للترحيلات أعلاه. جدول `__EFMigrationsHistory` هو المرجع الفعلي لما طبّقه EF Core على قاعدة البيانات. كذلك أعد تنفيذ `dotnet ef migrations list` وتأكد أن كل حالة Applied.

يمكن إنشاء SQL script للمراجعة قبل التطبيق:

```bash
dotnet ef migrations script --idempotent \
  --project Clinic.Infrastructure/Clinic.Infrastructure.csproj \
  --startup-project Clinic.Application/Clinic.Api.csproj \
  --output ClinicSystem-migrations.sql
```

الترحيل `ChecklistCoreRules` يجعل رسم التخصص الفرعي قابلًا لـ NULL ويضيف فهرسًا للمساعدة في فحص تداخل مواعيد المريض. لا تحذف قاعدة البيانات أو تعدّل migrations سبق تطبيقها على بيئة حقيقية.

## 5. بيانات التطوير التجريبية (Data Seeder)

الملف: `Clinic.Infrastructure/Persistence/DatabaseSeeder.cs`.

Seeder آمن التكرار (Idempotent) ويعمل فقط في بيئة `Development` وبعد تفعيل `SeedDemoData=true`. عند تشغيله:

- يطبق migrations تلقائيًا.
- ينشئ أدوار Identity: Admin وReceptionist وDoctor وPatient.
- ينشئ حسابات تجريبية وملفات طبيب ومريض.
- ينشئ تخصصًا رئيسيًا برسوم 100، تخصص أطفال فرعيًا بلا رسم لاختبار الوراثة، خدمة مختبر برسوم 25، وساعات دوام للطبيب من الاثنين إلى الجمعة 08:00–17:00 UTC.
- يطبع معرفات الطبيب والمريض والتخصصات والخدمة في سجل تشغيل API.

### تشغيل Seeder يدويًا لمرة تطويرية

في macOS/Linux:

```bash
ASPNETCORE_ENVIRONMENT=Development SeedDemoData=true \
  dotnet run --project Clinic.Application/Clinic.Api.csproj
```

في PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:SeedDemoData = "true"
dotnet run --project Clinic.Application/Clinic.Api.csproj
```

التشغيل العادي لا ينفذ Seeder؛ القيمة الافتراضية `SeedDemoData` هي false. لا تستخدم Seeder في Production، ولا تستخدم الحسابات التالية خارج قاعدة تطوير محلية:

| الدور | البريد | كلمة المرور المحلية |
|---|---|---|
| Admin | `admin@clinic.local` | `ClinicDemo123!` |
| Receptionist | `reception@clinic.local` | `ClinicDemo123!` |
| Doctor | `doctor@clinic.local` | `ClinicDemo123!` |
| Patient | `patient@clinic.local` | `ClinicDemo123!` |

غيّر بيانات الدخول واحذف قاعدة Demo قبل مشاركة البيئة. الـ Seeder لا ينشئ موعدًا جاهزًا؛ استخدم المعرفات التي يطبعها في خطوات الاختبار.

## 6. تشغيل API وSwagger

شغّل في وضع Development:

```bash
dotnet run --project Clinic.Application/Clinic.Api.csproj
```

حسب إعدادات `launchSettings.json`، العنوان عادةً:

- Swagger: `http://localhost:5219/swagger`
- HTTPS: `https://localhost:7000/swagger`

إذا لم يفتح Swagger، تحقق أن `ASPNETCORE_ENVIRONMENT=Development`؛ في هذا المشروع `UseSwagger()` و`UseSwaggerUI()` يعملان في Development فقط.

### استعمال زر Authorize

1. نفّذ `POST /api/Auth/login` من Swagger.
2. انسخ `token` من استجابة JSON، لا `refreshToken`.
3. اضغط **Authorize** والصق قيمة JWT فقط، من دون كتابة `Bearer `.
4. Swagger يضيف `Authorization: Bearer <token>` للعمليات المحمية تلقائيًا.

## 7. اختبار النظام من Swagger أو Postman

ابدأ بتشغيل Seeder. استخدم المعرفات التي ظهرت في سجل التطبيق. تأكد أن موعد الاختبار مستقبلًا ويقع من الاثنين إلى الجمعة بين 08:00 و17:00 UTC. تاريخ المثال أدناه يوم اثنين مستقبلًا وقت كتابة README، غيّره عند الحاجة.

### 7.1 تسجيل الدخول كمريض

`POST /api/Auth/login`

```json
{
  "email": "patient@clinic.local",
  "password": "ClinicDemo123!"
}
```

خذ `token` وفعّل Authorize. يمكن تجربة الحسابات الأخرى بنفس كلمة المرور:

- `doctor@clinic.local`
- `reception@clinic.local`
- `admin@clinic.local`

### 7.2 إنشاء موعد وحساب رسم التخصص الأب

`POST /api/Appointments` — يحتاج دور Patient أو Receptionist أو Admin. عوّض المعرفات بالقيم التي طبعها Seeder:

```json
{
  "patientId": "<PatientId>",
  "doctorId": "<DoctorId>",
  "specialtyId": "<ChildSpecialtyId>",
  "startTimeUtc": "2030-01-07T10:00:00Z",
  "durationMinutes": 30,
  "selectedServices": [
    {
      "serviceItemId": "<DemoServiceId>",
      "quantity": 1
    }
  ]
}
```

استخدام `ChildSpecialtyId` يختبر أخذ رسم التخصص الأب إذا كان رسم الفرعي NULL. إجمالي المثال المتوقع `125.00` (100 رسم استشارة + 25 خدمة). احتفظ بـ `id` المعاد من الاستجابة لاختبار الخطوات التالية.

### 7.3 تأكيد الموعد وإكماله

`PATCH /api/Appointments/<AppointmentId>/status` — سجّل دخولًا بحساب الطبيب المرتبط بالموعد:

```json
{
  "newStatus": 2,
  "notes": "Confirmed for testing"
}
```

القيم: Scheduled=1، Confirmed=2، Completed=3، Cancelled=4. الطبيب يستطيع تأكيد/إكمال موعده المسند فقط؛ لا يستطيع إكمال الموعد مباشرة من Scheduled، يجب تأكيده أولًا.

### 7.4 تسجيل دفعتين جزئيتين

سجّل دخولًا كـ Receptionist أو Admin. المسار: `POST /api/Appointments/<AppointmentId>/payments`.

الدفعة الأولى:

```json
{
  "amount": 50,
  "paymentMethod": 1
}
```

ثم الثانية:

```json
{
  "amount": 75,
  "paymentMethod": 1
}
```

`paymentMethod`: Cash=1، Card=2، BankTransfer=3. المجموع يساوي 125. اختبر الرفض بإرسال مبلغ يتجاوز الرصيد المتبقي.

### 7.5 اختبار التداخل والصلاحيات

- أعد إنشاء موعد آخر للطبيب أو المريض نفسه في الوقت المتداخل؛ يجب رفضه بـ 400.
- جرّب حجز ملف مريض آخر بحساب Patient؛ يجب رفضه بـ 403.
- جرّب تعديل/تسجيل دفعة بحساب Patient؛ يجب رفضه بسياسة Staff.
- جرّب أن يغيّر طبيب حالة موعد غير مسند إليه؛ يجب رفضه.
- اختبر إلغاء موعد بحساب المريض قبل أكثر من ساعتين؛ يجب أن ينجح إذا كانت الحالة Scheduled/Confirmed. ضمن الساعتين يُرفض. موظف الاستقبال/المدير يستطيع إلغاءه قبل اكتماله.
- أعد استخدام Refresh Token بعد التجديد أو الإلغاء؛ يجب أن يُرفض.

## 8. واجهات API المتاحة

| الطريقة | المسار | الاستخدام |
|---|---|---|
| POST | `/api/Auth/login` | إصدار Access وRefresh Tokens |
| POST | `/api/Auth/refresh` | تدوير وتجديد الرموز |
| POST | `/api/Auth/revoke` | إبطال Refresh Token |
| POST | `/api/Appointments` | إنشاء موعد |
| PUT | `/api/Appointments/{id}` | تعديل وقت/مدة موعد (Staff) |
| PATCH | `/api/Appointments/{id}/status` | تغيير الحالة حسب الدور والملكية |
| POST | `/api/Appointments/{id}/payments` | تسجيل دفعة (Staff) |

لا توجد حاليًا واجهات GET لعرض قائمة/تفاصيل المواعيد أو المدفوعات.

## 9. ملاحظات عند الفشل

- **Connection refused / SQL Server error:** تحقق من خدمة SQL Server وسلسلة الاتصال وبيانات الدخول، ثم جرّب اتصال قاعدة البيانات قبل تشغيل Seeder.
- **401 Unauthorized:** استخدم Access Token حديثًا، وتأكد من `SecretKey` و`Issuer` و`Audience` وعدم انتهاء الرمز.
- **403 Forbidden:** تأكد أن المستخدم عضو بالدور الصحيح، وأنه يملك سجل Patient/Doctor المرتبط بالمورد.
- **Migration pending:** نفّذ `dotnet ef database update` ثم افحص `dbo.__EFMigrationsHistory`.
- **Appointment outside working hours:** استخدم توقيت UTC وموعدًا في يوم دوام من 08:00 إلى 17:00 UTC.
- **Appointment overlap/capacity:** جرّب وقتًا مختلفًا أو تحقق من المواعيد النشطة وسعة الطبيب اليومية.
