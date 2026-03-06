using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Patient_Access_Experian_Project_API.Data_Transfer_Objects;
using Patient_Access_Experian_Project_API.Services;
using Microsoft.EntityFrameworkCore;
using Patient_Access_Experian_Project_API.Data;

namespace Patient_Access_Experian_Project_API.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentService _service;
        private readonly PatientAccessDbContext _db;

        public AppointmentsController(AppointmentService service, PatientAccessDbContext db)
        {
            _service = service;
            _db = db;
        }

        /// <summary>
        /// Creates a new appointment for a patient if the requested time is within the provider's availability and does not conflict with existing appointments.
        /// </summary>
        /// <response code = "201">Appointment created.</response>
        /// <response code = "400">Invalid request (e.g., duration, non-UTC time).</response>
        /// <response code = "404">Clinic/provider/patient not found.</response>
        /// <response code = "409"> Scheduling conflict (double booking).</response>
        [HttpPost]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
        {
            var (success, error, appt) = await _service.CreateAsync(
                request.ClinicId,
                request.ProviderId,
                request.PatientId,
                request.StartUtc,
                request.DurationMinutes,
                ct);

            if (!success)
            {
                // Map error --> status code
                if (error is null) return BadRequest("Unknown error.");

                if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                    return NotFound(error);

                if (error.Contains("conflict", StringComparison.OrdinalIgnoreCase))
                    return Conflict(error);

                return BadRequest(error);
            }

            var response = new AppointmentResponse(
                appt!.Id,
                appt.ClinicId,
                appt.ProviderId,
                appt.PatientId,
                appt.StartUtc,
                appt.EndUtc,
                appt.Status.ToString()
                );

            return CreatedAtAction(nameof(GetById), new { appointmentId = appt.Id }, response);
        }

        /// <summary>
        /// Gets an appointment by its unique ID.
        /// </summary>
        [HttpGet("{appointmentId:guid}")]
        [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid appointmentId, CancellationToken ct)
        {
            var appt = await _db.Appointments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == appointmentId, ct);

            if (appt is null) return NotFound();

            var response = new AppointmentResponse(
                appt.Id,
                appt.ClinicId,
                appt.ProviderId,
                appt.PatientId,
                appt.StartUtc,
                appt.EndUtc,
                appt.Status.ToString()
                );

            return Ok(response);
        }

        /// <summary>
        /// Retrieves a list of appointments that match the specified optional filters for clinic, provider, and date
        /// range.
        /// </summary>
        /// <param name="clinicId">The optional unique identifier of the clinic to filter appointments. If specified, only appointments
        /// associated with this clinic are included.</param>
        /// <param name="providerId">The optional unique identifier of the provider to filter appointments. If specified, only appointments
        /// associated with this provider are included.</param>
        /// <param name="fromUtc">The optional start date and time, in UTC, to filter appointments. Only appointments starting on or after
        /// this date are returned.</param>
        /// <param name="toUtc">The optional end date and time, in UTC, to filter appointments. Only appointments starting before this date
        /// are returned.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An IActionResult containing a list of appointment responses that match the specified filters. Returns an
        /// empty list if no appointments are found.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<AppointmentResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> List(
            [FromQuery] Guid? clinicId,
            [FromQuery] Guid? providerId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            CancellationToken ct)
        {
            var query = _db.Appointments.AsNoTracking().AsQueryable();

            if (fromUtc.HasValue) query = query.Where(a => a.StartUtc >= fromUtc.Value);
            if (toUtc.HasValue) query = query.Where(a => a.StartUtc < toUtc.Value);

            var results = await query
                .OrderBy(a => a.StartUtc)
                .Select(a => new AppointmentResponse(
                    a.Id,
                    a.ClinicId,
                    a.ProviderId,
                    a.PatientId,
                    a.StartUtc,
                    a.EndUtc,
                    a.Status.ToString()
                 ))
                .ToListAsync(ct);
            return Ok(results);
        }


        /// <summary>
        /// Attempts to cancel the specified appointment if it has not already been completed.
        /// </summary>
        /// <remarks>Cancellation is not permitted for appointments that have already been completed. The
        /// method checks the appointment's status before performing the cancellation.</remarks>
        /// <param name="appointmentId">The unique identifier of the appointment to cancel.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>An IActionResult indicating the result of the cancellation request. Returns NoContent if the appointment is
        /// successfully canceled; NotFound if the appointment does not exist; or BadRequest if the appointment has
        /// already been completed.</returns>
        [HttpPost("{appointmentId:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel(Guid appointmentId, CancellationToken ct)
        {
            var appt = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
            if (appt is null) return NotFound();

            if (appt.Status == Models.AppointmentStatus.Completed)
                return BadRequest("Cannot cancel a completed appointment.");

            appt.Status = Models.AppointmentStatus.Cancelled;
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }


        /// <summary>
        /// Marks the specified appointment as completed if it exists and is not cancelled.
        /// </summary>
        /// <remarks>This method updates the status of an appointment to completed unless the appointment
        /// has already been cancelled. Attempting to complete a cancelled appointment results in a bad request
        /// response.</remarks>
        /// <param name="appointmentId">The unique identifier of the appointment to complete.</param>
        /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>An IActionResult indicating the outcome of the operation. Returns 204 No Content if the appointment is
        /// successfully completed; 404 Not Found if the appointment does not exist; or 400 Bad Request if the
        /// appointment is cancelled.</returns>
        [HttpPost("{appointmentId:guid}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Complete(Guid appointmentId, CancellationToken ct)
        {
            var appt = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, ct);
            if (appt is null) return NotFound();

            if (appt.Status == Models.AppointmentStatus.Cancelled)
                return BadRequest("Cannot complete a cancelled appointment.");

            appt.Status = Models.AppointmentStatus.Completed;
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }
    }
}
