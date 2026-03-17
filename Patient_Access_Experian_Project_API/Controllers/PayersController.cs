using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;
using Patient_Access_Experian_Project_API.Models;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/payers")]
    public class PayersController : ControllerBase
    {
        private readonly PatientAccessDbContext _db;
        public PayersController(PatientAccessDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var payers = await _db.Payers.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(ct);

            return Ok(payers);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] string name, CancellationToken ct)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest("Name is required.");

            var exists = await _db.Payers.AnyAsync(p => p.Name == name, ct);
            if (exists) return Conflict("Payer name already exists.");

            var payer = new Payer { Id = Guid.NewGuid(), Name = name };
            _db.Payers.Add(payer);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(List), new { id = payer.Id }, new { payer.Id, payer.Name });
        }
    }
}
