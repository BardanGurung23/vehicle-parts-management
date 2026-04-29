using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vpims.Application.DTOs.Auth;
using Vpims.Application.DTOs.Reviews;
using Vpims.Application.Interfaces.Services;

namespace Vpims.API.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(
    IServiceReviewService serviceReviewService,
    IAuthService authService) : ControllerBase
{
    [Authorize(Roles = "Customer")]
    [HttpPost]
    [ProducesResponseType<ReviewResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReviewResponse>> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        ReviewResponse response = await serviceReviewService.CreateReviewAsync(currentUser, request, cancellationToken);
        return Created($"/api/reviews/{response.ReviewId}", response);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("me")]
    [ProducesResponseType<IReadOnlyList<ReviewResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewResponse>>> GetMyReviews(CancellationToken cancellationToken)
    {
        UserProfileResponse currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        IReadOnlyList<ReviewResponse> response = await serviceReviewService.GetCustomerReviewsAsync(currentUser, cancellationToken);
        return Ok(response);
    }

    [HttpGet("appointment/{appointmentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReviewResponse>> GetByAppointmentId(int appointmentId, CancellationToken cancellationToken)
    {
        ReviewResponse? response = await serviceReviewService.GetReviewByAppointmentIdAsync(appointmentId, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }
        return Ok(response);
    }
}