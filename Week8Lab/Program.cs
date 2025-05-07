// ----- GEREKLİ USING İFADELERİ (En üste ekle veya kontrol et) -----
using Microsoft.EntityFrameworkCore;
using Week8Lab.Data; // SchoolDbContext'in namespace'i - Kendi projenle değiştir!
// Diğer mevcut using ifadeleri...

// ----- MEVCUT KOD BAŞLANGICI -----
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// *** Session servisini ekle ***
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// *** SON: Session servisi ***

// ---------- YENİ EKLENEN VERİTABANI SERVİSİ ----------
// Veritabanı bağlamını (DbContext) ekle ve yapılandır
builder.Services.AddDbContext<SchoolDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SchoolDbConnection")));
// ---------- SON: VERİTABANI SERVİSİ ----------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    // app.UseHsts(); // HSTS genellikle production'da etkinleştirilir
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Statik dosyalar için

// *** Session middleware'ini ekle (UseRouting'den ÖNCE!) ***
// ÖNEMLİ NOT: UseSession çağrısı UseRouting'den önce, UseAuthorization'dan sonra olabilir
// veya UseStaticFiles'dan sonra UseRouting'den önce olabilir. Genellikle UseRouting'den hemen önce tavsiye edilir.
// Ancak bazı senaryolarda UseAuthorization'dan sonra da çalışabilir. Mevcut yerinde bırakalım.
app.UseSession();

app.UseRouting();

app.UseAuthorization(); // Eğer kimlik doğrulama/yetkilendirme kullanıyorsanız

app.MapRazorPages();

app.Run();