using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Queries;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentValidation.Results;
using MediatR;
using RoomAggregate = Epam.ItMarathon.ApiService.Domain.Aggregate.Room.Room;

namespace Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers
{
    /// <summary>
    /// Handler for deleting users from a room.
    /// </summary>
    /// <param name="roomRepository">Repository for room operations.</param>
    public class DeleteUsersHandler(IRoomRepository roomRepository)
        : IRequestHandler<DeleteUsersRequest, Result<RoomAggregate, ValidationResult>>
    {
        ///<inheritdoc/>
        public async Task<Result<RoomAggregate, ValidationResult>> Handle(DeleteUsersRequest request,
            CancellationToken cancellationToken)
        {
            // Get room by auth user's code
            var roomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            if (roomResult.IsFailure)
            {
                return roomResult;
            }

            var room = roomResult.Value;

            // Find auth user by code
            var authUser = room.Users.FirstOrDefault(u => u.AuthCode == request.UserCode);
            if (authUser is null)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new NotFoundError(new[]
                {
                    new ValidationFailure("userCode", "User with specified userCode was not found.")
                }));
            }

            // userCode is not administrator's.
            if (!authUser.IsAdmin)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new ForbiddenError(new[]
                {
                    new ValidationFailure("userCode", "Only admin can delete users.")
                }));
            }

            // Validate provided UserId
            if (request.UserId is null)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError(new[]
                {
                    new ValidationFailure("id", "User Id must be provided.")
                }));
            }

            // The user with userCode and id is the same user
            if (authUser.Id == request.UserId.Value)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError(new[]
                {
                    new ValidationFailure("id", "Admin cannot delete themself.")
                }));
            }

            // User with userCode and id belong to different rooms
            var userToDelete = room.Users.FirstOrDefault(u => u.Id == request.UserId.Value);
            if (userToDelete is null)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new NotFoundError(new[]
                {
                    // Message aligned with existing tests expecting "User not found in the room."
                    new ValidationFailure("id", "User not found in the room.")
                }));
            }

            // The room is already closed
            if (room.ClosedOn is not null)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError(new[]
                {
                    new ValidationFailure("room.ClosedOn", "Room is already closed.")
                }));
            }

            // Delete user
            var deleteResult = room.DeleteUser(request.UserId.Value);
            if (deleteResult.IsFailure)
            {
                return deleteResult;
            }

            var updateResult = await roomRepository.UpdateAsync(room, cancellationToken);
            if (updateResult.IsFailure)
            {
                return Result.Failure<RoomAggregate, ValidationResult>(new BadRequestError([
                    new ValidationFailure(string.Empty, updateResult.Error)
                ]));
            }

            var updatedRoomResult = await roomRepository.GetByUserCodeAsync(request.UserCode, cancellationToken);
            return updatedRoomResult;
        }
    }
}
