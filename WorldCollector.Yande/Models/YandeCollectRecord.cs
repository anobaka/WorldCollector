using System;
using System.ComponentModel.DataAnnotations;

namespace WorldCollector.Yande.Models
{
    public class YandeCollectRecord
    {
        [Key]
        public DateTime CollectDt { get; set; }

        public string Ids { get; set; }

        [Required(AllowEmptyStrings = false), MaxLength(16)]
        public string Site { get; set; }
    }
}