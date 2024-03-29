﻿namespace MinimalEndpoints.Extensions;

public interface IHaveProblemDetails
{
    string Type { get; }
    string Detail { get; }
    string Title { get; }
    string Instance { get; }
    int Status { get; }
}

public interface IHaveValidationProblemDetails: IHaveProblemDetails
{
    IDictionary<string, string[]> Errors { get; }
}
