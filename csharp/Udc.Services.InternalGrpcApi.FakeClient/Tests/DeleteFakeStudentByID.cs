﻿using Grpc.Core;
using Grpc.Net.Client;
using Udc.Services.InternalGrpcApi.FakeClient.Builders;
using Udc.Services.InternalGrpcApi.FakeClient.Extensions;
using Udc.Services.InternalGrpcApi.FakeClient.Helpers;
using Udc.Services.InternalGrpcApi.FakeClient.Providers;
using Udc.Services.Protos.Fake;
using Xunit;
using Xunit.Abstractions;

namespace Udc.Services.InternalGrpcApi.FakeClient.Tests;

public class DeleteFakeStudentById : BaseTest, IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly FakeService.FakeServiceClient _client;
    private readonly ITestOutputHelper _output;
    private FakeStudentBuilder FakeStudentBuilder { get; } = new();

    public DeleteFakeStudentById(ITestOutputHelper output, ITokenProvider tokenProvider)
    {
        _output = output;
        _channel = Task.Run<GrpcChannel>(async () => await ServiceHelpers.CreateAuthenticatedChannelAsync(RequiredAuthenticatedServiceUrl, tokenProvider)).Result;
        _client = new FakeService.FakeServiceClient(_channel);
    }

    [Fact]
    public async Task DeletesExistingFakeStudentById_ThroughTo_InternalFakeGrpcService_Async()
    {
        var newFakeStudent = FakeStudentBuilder.WithRandomValuesAndNif();
        CreateFakeStudentRequest newFakeStudentRequest = new() { FakeStudent = newFakeStudent };
        var newFakeStudentIdResponse = await _client.CreateFakeStudentAsync(newFakeStudentRequest, deadline: DateTime.UtcNow.AddMinutes(1));
        newFakeStudent.FakeBasePerson.FakeBaseEntity.Id = newFakeStudentIdResponse.Id;
        _output.WriteLine($"NewFakeStudentId: {newFakeStudent.FakeBasePerson.FakeBaseEntity.Id}");

        DeleteFakeStudentByIdRequest deleteRequest = new() { Id = newFakeStudent.FakeBasePerson.FakeBaseEntity.Id };
        await _client.DeleteFakeStudentByIdAsync(deleteRequest, deadline: DateTime.UtcNow.AddMinutes(1));

        GetFakeStudentByIdRequest getRequest = new() { Id = newFakeStudent.FakeBasePerson.FakeBaseEntity.Id };
        var ex = await Assert.ThrowsAsync<RpcException>(async () => await _client.GetFakeStudentByIdAsync(getRequest, deadline: DateTime.UtcNow.AddMinutes(1)));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CantDeleteNotExistingFakeStudentById_ThroughTo_InternalFakeGrpcService_Async()
    {
        DeleteFakeStudentByIdRequest request = new() { Id = FakeStudentBuilder.TestNotExistingId };

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await _client.DeleteFakeStudentByIdAsync(request, deadline: DateTime.UtcNow.AddMinutes(1)));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task CantValidateEmptyId_In_DeleteFakeStudentById_ThroughTo_InternalFakeGrpcService_Async()
    {
        DeleteFakeStudentByIdRequest request = new() { Id = " " };

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await _client.DeleteFakeStudentByIdAsync(request, deadline: DateTime.UtcNow.AddMinutes(1)));

        var errors = ex.GetValidationErrors();
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.NotEmpty(errors);
        Assert.All<ValidationTrailer>(errors, error => Assert.Equal(nameof(request.Id), error.PropertyName, ignoreCase: true));
    }

    [Fact]
    public async Task CantValidateInvalidId_In_DeleteFakeStudentById_ThroughTo_InternalFakeGrpcService_Async()
    {
        DeleteFakeStudentByIdRequest request = new() { Id = FakeStudentBuilder.TestInvalidId };

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await _client.DeleteFakeStudentByIdAsync(request, deadline: DateTime.UtcNow.AddMinutes(1)));

        var errors = ex.GetValidationErrors();
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
        Assert.NotEmpty(errors);
        Assert.All<ValidationTrailer>(errors, error => Assert.Equal(nameof(request.Id), error.PropertyName, ignoreCase: true));
    }

    public void Dispose()
    {
        if (_channel is not null)
        {
            _channel.Dispose();
        }
    }
}

