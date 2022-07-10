﻿namespace MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class EndpointAttribute : Attribute
{
    public string? TagName { get; set; }
    public string? OperatinId { get; set; }
    public bool ExcludeFromDescription { get; set; }
}