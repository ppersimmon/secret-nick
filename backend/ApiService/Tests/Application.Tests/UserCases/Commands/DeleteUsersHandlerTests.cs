using CSharpFunctionalExtensions;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Commands;
using Epam.ItMarathon.ApiService.Application.UseCases.User.Handlers;
using Epam.ItMarathon.ApiService.Domain.Abstract;
using Epam.ItMarathon.ApiService.Domain.Aggregate.Room;
using Epam.ItMarathon.ApiService.Domain.Entities.User;
using Epam.ItMarathon.ApiService.Domain.Shared.ValidationErrors;
using FluentAssertions;
using FluentValidation.Results;
using NSubstitute;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace Epam.ItMarathon.ApiService.Application.Tests.UserCases.Commands
{
    /// <summary>
    /// Unit tests for the <see cref="DeleteUsersHandler"/> class.
    /// </summary>
    public class DeleteUsersHandlerTests
    {
        private readonly IRoomRepository _roomRepositoryMock;
        private readonly DeleteUsersHandler _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteUsersHandlerTests"/> class with mocked dependencies.
        /// </summary>
        public DeleteUsersHandlerTests()
        {
            _roomRepositoryMock = Substitute.For<IRoomRepository>();
            _handler = new DeleteUsersHandler(_roomRepositoryMock);
        }

        /// <summary>
        /// Tests that the handler successfully deletes a user when the request is valid and the user is an admin.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnSuccess_When_RequestIsValidAndUserIsAdmin()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithIsAdmin(true)
                .Build();

            var userToDelete = DataFakers.ValidUserBuilder
                .WithId(999ul) // Give a distinct ID
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<User> { adminUser, userToDelete })
                .RuleFor(r => r.ClosedOn, _ => (DateTime?)null)
                .Generate();

            var request = new DeleteUsersRequest(adminUser.AuthCode, userToDelete.Id);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            _roomRepositoryMock.UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            await _roomRepositoryMock.Received(1).UpdateAsync(
                Arg.Is<Room>(r => !r.Users.Contains(userToDelete)),
                Arg.Any<CancellationToken>());

            room.Users.Should().NotContain(userToDelete);

            await _roomRepositoryMock.Received(2).GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a NotFoundError when the room for the userCode is not found.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnFailure_When_RoomIsNotFound()
        {
            // Arrange
            var request = new DeleteUsersRequest("invalid_code", 1);
            var notFoundError = new NotFoundError([new ValidationFailure("userCode", "Room not found")]);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Failure<Room, ValidationResult>(notFoundError));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            await _roomRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a ForbiddenError when the authenticated user is not an admin.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnForbidden_When_AuthUserIsNotAdmin()
        {
            // Arrange
            var notAdminUser = DataFakers.ValidUserBuilder
                .WithIsAdmin(false)
                .Build();

            var userToDelete = DataFakers.ValidUserBuilder.Build();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<User> { notAdminUser, userToDelete })
                .Generate();

            var request = new DeleteUsersRequest(notAdminUser.AuthCode, userToDelete.Id);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<ForbiddenError>();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage == "Only admin can delete users.");
            await _roomRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when the admin tries to delete themself.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_AdminTriesToDeleteThemself()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithIsAdmin(true)
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<User> { adminUser })
                .Generate();

            var request = new DeleteUsersRequest(adminUser.AuthCode, adminUser.Id);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage == "Admin cannot delete themself.");
            await _roomRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a NotFoundError when the user specified by 'id' is not in the room.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnNotFound_When_UserToDeleteIsNotFoundInRoom()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithIsAdmin(true)
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<User> { adminUser })
                .Generate();

            var nonExistentUserId = 999ul;
            var request = new DeleteUsersRequest(adminUser.AuthCode, nonExistentUserId);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<NotFoundError>();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage == "User not found in the room.");
            await _roomRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when the room is already closed.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_RoomIsAlreadyClosed()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithIsAdmin(true)
                .Build();

            var userToDelete = DataFakers.ValidUserBuilder
                .WithId(999ul)
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<User> { adminUser, userToDelete })
                .RuleFor(r => r.ClosedOn, _ => DateTime.UtcNow)
                .Generate();

            var request = new DeleteUsersRequest(adminUser.AuthCode, userToDelete.Id);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage == "Room is already closed.");
            await _roomRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }

        /// <summary>
        /// Tests that the handler returns a BadRequestError when the UserId in request is null.
        /// </summary>
        [Fact]
        public async Task Handle_Should_ReturnBadRequest_When_UserIdIsNull()
        {
            // Arrange
            var adminUser = DataFakers.ValidUserBuilder
                .WithIsAdmin(true)
                .Build();

            var room = DataFakers.RoomFaker
                .RuleFor(r => r.Users, _ => new List<User> { adminUser })
                .Generate();

            // Передаем null в UserId
            var request = new DeleteUsersRequest(adminUser.AuthCode, null);

            _roomRepositoryMock.GetByUserCodeAsync(request.UserCode, Arg.Any<CancellationToken>())
                .Returns(Result.Success<Room, ValidationResult>(room));

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.IsFailure.Should().BeTrue();
            result.Error.Should().BeOfType<BadRequestError>();
            result.Error.Errors.Should().Contain(e => e.ErrorMessage == "User Id must be provided.");
            await _roomRepositoryMock.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
        }
    }
}
