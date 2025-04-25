var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// *** YENİ: Session servisini ekle ***
builder.Services.AddSession(options =>
{
    // Oturumun ne kadar süre boşta kalabileceğini ayarla (30 dakika)
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    // Cookie'nin sadece sunucu tarafından erişilebilir olmasını sağla (client-side script erişemez)
    options.Cookie.HttpOnly = true;
    // Session cookie'sinin uygulamanın çalışması için gerekli olduğunu belirt
    // (GDPR onay mekanizmalarıyla ilgili)
    options.Cookie.IsEssential = true;
});
// *** SON: Session servisi ***

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. 
}

app.UseHttpsRedirection();

// Statik dosyaların sunulması için (CSS, JS, Resimler vb.)
// Eğer MapStaticAssets() bunu zaten yapıyorsa, UseStaticFiles() gerekmeyebilir,
// ancak genellikle UseStaticFiles() burada bulunur.
app.UseStaticFiles(); // <-- Genellikle burada yer alır, MapStaticAssets ile çakışıp çakışmadığını kontrol edin.

// *** YENİ: Session middleware'ini ekle (UseRouting'den ÖNCE!) ***
app.UseSession();
// *** SON: Session middleware'i ***

app.UseRouting();

app.UseAuthorization(); // Eğer kimlik doğrulama/yetkilendirme kullanıyorsanız

app.MapRazorPages();

app.Run();
