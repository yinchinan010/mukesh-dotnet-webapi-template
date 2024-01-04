﻿using FluentValidation;
using FSH.WebApi.Todo.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Todo.Features.Creation.v1;
public class TodoCreationValidator : AbstractValidator<TodoCreationCommand>
{
    public TodoCreationValidator(TodoDbContext context)
    {
        RuleFor(p => p.Title)
            .NotEmpty()
            .MustAsync(async (title, token) => await context.Todos.AllAsync(a => a.Title != title, token).ConfigureAwait(false))
            .WithMessage((_, name) => "todo item title '{PropertyValue}' already exists.");

        RuleFor(p => p.Note).NotEmpty();
    }
}
