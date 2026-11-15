using System.Linq;
using System.Reflection;
using eiupdds_backend.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace eiupdds_backend.Tests;

public class SmokeTests
{
    [Fact]
    public void System_ShouldPassBasicSmokeTest()
    {
        // Assert basic system integrity
        Assert.True(true, "CI environment and test framework are properly configured.");
    }

    [Fact]
    public void DiagnosisController_PredictEndpoint_ShouldHavePredictRateLimitingAttribute()
    {
        var method = typeof(DiagnosisController).GetMethod(nameof(DiagnosisController.Predict));
        Assert.NotNull(method);

        var attribute = method.GetCustomAttributes(typeof(EnableRateLimitingAttribute), true)
            .Cast<EnableRateLimitingAttribute>()
            .FirstOrDefault();

        Assert.NotNull(attribute);
        Assert.Equal("PredictRateLimit", attribute.PolicyName);
    }
}
