using System.ComponentModel.DataAnnotations;

namespace Week5Lab.Models // ❗️Burada projenin adını tam olarak yazmalısın
{
    public class ClassInformationModel
    {
        public int Id { get; set; }

        [Required]
        public string ClassName { get; set; }

        [Required]
        [Range(1, 1000)]
        public int StudentCount { get; set; }

        public string Description { get; set; }
    }
}
