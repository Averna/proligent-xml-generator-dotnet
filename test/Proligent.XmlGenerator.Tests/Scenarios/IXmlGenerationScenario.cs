namespace Proligent.XmlGenerator.Tests.Scenarios;

public sealed record ScenarioResult(DataWareHouse Warehouse, Util Util);

public interface IXmlGenerationScenario
{
    ScenarioResult Generate(DateTime? startTimestamp = null);
}
