using BeFitAPI.Data;
using BeFitAPI.DTOs;
using BeFitAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeFitAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ExercisesController(ApplicationDbContext context) : ControllerBase
{
    // The exercises the controller works on, with their types - every query starts here.
    private IQueryable<Exercise> Exercises => context.Exercises.Include(e => e.ExerciseType);

    // GET: api/Exercises?sessionId=5
    [HttpGet]
    public async Task<IEnumerable<ShowExercise>> GetExercises(int? sessionId)
    {
        var exercises = Exercises;
        if (sessionId != null)
        {
            exercises = exercises.Where(e => e.SessionId == sessionId);
        }
        return await exercises.OrderBy(e => e.Id).Select(e => new ShowExercise(e)).ToListAsync();
    }

    // GET: api/Exercises/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ShowExercise>> GetExercise(int id)
    {
        var exercise = await Exercises.FirstOrDefaultAsync(e => e.Id == id);
        return exercise == null ? NotFound() : new ShowExercise(exercise);
    }

    // POST: api/Exercises
    [HttpPost]
    public async Task<ActionResult<ShowExercise>> PostExercise(CreateExercise input)
    {
        if (await CheckReferencesAsync(input) is { } problem)
        {
            return problem;
        }

        var exercise = new Exercise();
        Copy(input, exercise);
        context.Exercises.Add(exercise);
        await context.SaveChangesAsync();
        await context.Entry(exercise).Reference(e => e.ExerciseType).LoadAsync();

        return CreatedAtAction(nameof(GetExercise), new { id = exercise.Id }, new ShowExercise(exercise));
    }

    // PUT: api/Exercises/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutExercise(int id, CreateExercise input)
    {
        var exercise = await Exercises.FirstOrDefaultAsync(e => e.Id == id);
        if (exercise == null)
        {
            return NotFound();
        }
        if (await CheckReferencesAsync(input) is { } problem)
        {
            return problem;
        }

        Copy(input, exercise);
        await context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/Exercises/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExercise(int id)
    {
        var exercise = await Exercises.FirstOrDefaultAsync(e => e.Id == id);
        if (exercise == null)
        {
            return NotFound();
        }

        context.Exercises.Remove(exercise);
        await context.SaveChangesAsync();
        return NoContent();
    }

    // The session and the exercise type must exist - otherwise 400 in the same format as the validation errors.
    private async Task<ActionResult?> CheckReferencesAsync(CreateExercise input)
    {
        if (!await context.Sessions.AnyAsync(s => s.Id == input.SessionId))
        {
            ModelState.AddModelError(nameof(input.SessionId), "Nie ma takiej sesji.");
        }
        if (!await context.ExerciseTypes.AnyAsync(t => t.Id == input.ExerciseTypeId))
        {
            ModelState.AddModelError(nameof(input.ExerciseTypeId), "Nie ma takiego typu ćwiczenia.");
        }
        return ModelState.IsValid ? null : ValidationProblem(ModelState);
    }

    private static void Copy(CreateExercise input, Exercise exercise)
    {
        exercise.ExerciseTypeId = input.ExerciseTypeId!.Value;
        exercise.SessionId = input.SessionId!.Value;
        exercise.Weight = input.Weight!.Value;
        exercise.NumOfReps = input.NumOfReps!.Value;
        exercise.NumOfSeries = input.NumOfSeries!.Value;
        exercise.Comment = input.Comment;
    }
}
