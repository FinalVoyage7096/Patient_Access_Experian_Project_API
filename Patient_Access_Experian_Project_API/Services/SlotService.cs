using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Services
{
    public class SlotService
    {




        private static DateTime Max(DateTime a, DateTime b) => a >= b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a <= b ? a : b;
    }
}
