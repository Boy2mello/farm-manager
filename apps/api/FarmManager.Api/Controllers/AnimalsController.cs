using FarmManager.Application.Animals.Commands.RegisterAnimal;
using FarmManager.Application.Animals.Commands.UpdateAnimal;
using FarmManager.Application.Animals.Queries;
using FarmManager.Application.Calvings.Commands.RecordCalving;
using FarmManager.Domain.Animals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmManager.Api.Controllers;

[ApiController]
[Route("api/v1/animals")]
[Authorize]
public sealed class AnimalsController(ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, [FromQuery] AnimalStatus? status, CancellationToken ct)
    {
        var animals = await mediator.Send(new ListAnimalsQuery(search, status), ct);
        return Ok(animals);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var animal = await mediator.Send(new GetAnimalByIdQuery(id), ct);
        return animal is null ? NotFound() : Ok(animal);
    }

    public sealed record UpdateAnimalRequest(
        AnimalStatus? Status,
        string? PrimaryName,
        IReadOnlyList<string>? Aliases,
        DateOnly? WithdrawalUntil,
        DateOnly? DisposalDate,
        string? DisposalReason);

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAnimalRequest body, CancellationToken ct)
    {
        await mediator.Send(new UpdateAnimalCommand(
            id, body.Status, body.PrimaryName, body.Aliases,
            body.WithdrawalUntil, body.DisposalDate, body.DisposalReason), ct);
        return NoContent();
    }

    public sealed record RegisterAnimalRequest(
        string? PrimaryName,
        AnimalSex Sex,
        DateOnly Dob,
        DobPrecision DobPrecision,
        AnimalSource Source,
        Guid? DamId,
        Guid? SireId,
        Guid? BreedId,
        Guid? FarmId,
        IReadOnlyList<string>? Aliases);

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterAnimalRequest body, CancellationToken ct)
    {
        var cmd = new RegisterAnimalCommand(
            body.PrimaryName, body.Sex, body.Dob, body.DobPrecision, body.Source,
            body.DamId, body.SireId, body.BreedId, body.FarmId, body.Aliases);

        var result = await mediator.Send(cmd, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.AnimalId },
            new { animalId = result.AnimalId, codeName = result.CodeName });
    }

    public sealed record RecordCalvingRequest(
        Guid DamId,
        DateOnly CalvingDate,
        AnimalSex CalfSex,
        Guid? SireId,
        string? SireExternalNote,
        int DifficultyScore,
        bool AssistanceRequired,
        bool PlacentaDelivered,
        int? MotheringAbility,
        bool Stillbirth,
        decimal? CalfWeightKg,
        int? CalfVigour,
        string? Notes);

    [HttpPost("calvings")]
    public async Task<IActionResult> RecordCalving([FromBody] RecordCalvingRequest body, CancellationToken ct)
    {
        var cmd = new RecordCalvingCommand(
            body.DamId, body.CalvingDate, body.CalfSex, body.SireId, body.SireExternalNote,
            body.DifficultyScore, body.AssistanceRequired, body.PlacentaDelivered,
            body.MotheringAbility, body.Stillbirth, body.CalfWeightKg, body.CalfVigour, body.Notes);

        var result = await mediator.Send(cmd, ct);
        return Ok(new
        {
            calvingEventId = result.CalvingEventId,
            calfId = result.CalfId,
            calfCodeName = result.CalfCodeName,
        });
    }
}
