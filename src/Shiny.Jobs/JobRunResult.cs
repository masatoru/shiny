using System;

namespace Shiny.Jobs;


public record JobRunResult(
    IJob Job, 
    Exception? Exception
)
{
    public bool Success => this.Exception == null;
};