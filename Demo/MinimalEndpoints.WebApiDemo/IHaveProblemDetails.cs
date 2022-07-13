﻿namespace MinimalEndpoints.WebApiDemo;

public interface IHaveProblemDetails
{ 
    string Type { get; }
    string Detail { get; }
    string Title { get; }
    string Instance { get; }
    int Status { get; }
}
