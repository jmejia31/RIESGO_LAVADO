using Xunit;
using RL.API.Infrastructure.Reporting;

namespace RL.API.Tests.Infrastructure.Reporting;

public sealed class InstitutionalReportStandardTests
{
    [Fact]
    public void Palette_UsesApprovedInstitutionalColors()
    {
        Assert.Equal("#123B63", InstitutionalReportStandard.Palette.Navy);
        Assert.Equal("#FFFFFF", InstitutionalReportStandard.Palette.HeaderText);
        Assert.Equal("#F3F6F9", InstitutionalReportStandard.Palette.AlternateRow);
    }

    [Theory]
    [InlineData(6, InstitutionalReportOrientation.Portrait)]
    [InlineData(8, InstitutionalReportOrientation.Portrait)]
    [InlineData(9, InstitutionalReportOrientation.Landscape)]
    [InlineData(14, InstitutionalReportOrientation.Landscape)]
    public void ResolveOrientation_DependsOnVisibleColumns(int columns, InstitutionalReportOrientation expected)
    {
        Assert.Equal(expected, InstitutionalReportStandard.ResolveOrientation(columns));
    }

    [Theory]
    [InlineData(100, 20, 140, false)]
    [InlineData(125, 20, 140, true)]
    public void MustMoveRowToNextPage_PreventsSplitRows(decimal currentY, decimal rowHeight, decimal footerStartY, bool expected)
    {
        Assert.Equal(expected, InstitutionalReportStandard.MustMoveRowToNextPage(currentY, rowHeight, footerStartY));
    }

    [Fact]
    public void ValidateMetadata_RejectsMissingTitle()
    {
        var metadata = new InstitutionalReportMetadata("", "Matrices de Riesgos", DateTime.UtcNow);
        Assert.Throws<ArgumentException>(() => InstitutionalReportStandard.ValidateMetadata(metadata));
    }
}
