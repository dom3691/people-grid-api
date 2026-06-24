using Microsoft.AspNetCore.Authorization;

namespace PeopleGrid.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute(string permission) : AuthorizeAttribute(policy: $"Permission:{permission}");
