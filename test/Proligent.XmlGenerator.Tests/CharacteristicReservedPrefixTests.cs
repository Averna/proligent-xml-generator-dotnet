using Xunit;

namespace Proligent.XmlGenerator.Tests;

public class CharacteristicReservedPrefixTests
{
    [Fact]
    public void StepRun_Rejects_ReservedCharacteristic()
    {
        Assert.Throws<ArgumentException>(() =>
            new StepRun(characteristics: new[] { new Characteristic("Proligent.Custom") })
        );
    }

    [Fact]
    public void SequenceRun_AddCharacteristic_RejectsReservedPrefix()
    {
        var sequence = new SequenceRun();
        Assert.Throws<ArgumentException>(() =>
            sequence.AddCharacteristic(new Characteristic("Proligent.Custom"))
        );
    }

    [Fact]
    public void OperationRun_RejectsReservedCharacteristic()
    {
        Assert.Throws<ArgumentException>(() =>
            new OperationRun(
                "Station/A",
                characteristics: new[] { new Characteristic("Proligent.Custom") }
            )
        );
    }

    [Fact]
    public void ProductUnit_AddCharacteristic_RejectsReservedPrefix()
    {
        ProductUnit productUnit = new ProductUnit();
        Assert.Throws<ArgumentException>(() =>
            productUnit.AddCharacteristic(new Characteristic("Proligent.Custom"))
        );
    }
}
