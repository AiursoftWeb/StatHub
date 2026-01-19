using System.Diagnostics.CodeAnalysis;
using Aiursoft.StatHub.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.StatHub.MySql;

[ExcludeFromCodeCoverage]

public class MySqlContext(DbContextOptions<MySqlContext> options) : StatHubDbContext(options);
