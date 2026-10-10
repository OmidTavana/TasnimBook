using System;
using System.Collections.Generic;
using System.Text;

namespace TasnimBook.Data.Models
{
    public class DocumentUploadResponse
    {
        public bool IsUploaded { get; set; }
        public string Message { get; set; } = "";
        public List<string> DocumentUrls { get; set; } = new List<string>();
    }
}
