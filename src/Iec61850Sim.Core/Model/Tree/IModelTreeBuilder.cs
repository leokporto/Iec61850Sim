using IEC61850.Server;
using Iec61850Sim.Core.Points;

namespace Iec61850Sim.Core.Model.Tree;

public interface IModelTreeBuilder
{
    IModelNode Build(string iedName, IedModel model, IPointRegistry registry);
}