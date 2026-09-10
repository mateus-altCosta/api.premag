using FluentAssertions;
using Premag.Core;

namespace Premag.UnitTests;

public class JpegInfoTests
{
    [Fact]
    public void AssinaturaJpeg_ReconheceSoi()
    {
        JpegInfo.EhJpeg([0xFF, 0xD8, 0xFF, 0xE0]).Should().BeTrue();
        JpegInfo.EhJpeg([0x89, 0x50, 0x4E, 0x47]).Should().BeFalse();
    }
}
