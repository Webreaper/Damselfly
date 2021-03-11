using System.ComponentModel.DataAnnotations;

namespace Damselfly.Core.Models
{
    public class WordpressSettings
    {
        [Required]
        [Url]
        public string URL { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }

}
