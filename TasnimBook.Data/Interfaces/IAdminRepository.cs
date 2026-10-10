using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TasnimBook.Data.Interfaces
{
    public interface IAdminRepository
    {
        Task<string> UploadFile(byte[] file, string name);
    }
}
