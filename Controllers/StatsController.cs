using BeFitAPI.Data;
using BeFitAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeFitAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StatsController(ApplicationDbContext context) : ControllerBase
{
    // GET: api/Stats?weeks=4 - for every exercise type of the exercises from the last weeks.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ShowStats>>> GetStats(int weeks = 4)
    {
        if (weeks is < 1 or > 52)
        {
            return BadRequest("Okres musi wynosić od 1 do 52 tygodni.");
        }

        var since = DateTime.Today.AddDays(-7 * weeks);
        return await context.Exercises
            .Where(e => e.Session!.Start >= since)
            .GroupBy(e => e.ExerciseType!.Name)
            .Select(g => new ShowStats
            {
                ExerciseTypeName = g.Key,
                Count = g.Count(),
                TotalReps = g.Sum(e => e.NumOfReps * e.NumOfSeries),
                AverageWeight = g.Average(e => e.Weight),
                MaxWeight = g.Max(e => e.Weight)
            })
            .OrderBy(s => s.ExerciseTypeName)
            .ToListAsync();
    }
}