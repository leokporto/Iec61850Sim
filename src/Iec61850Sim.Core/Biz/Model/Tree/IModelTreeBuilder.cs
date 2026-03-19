using IEC61850.Server;
using Iec61850Sim.Core.Biz.Points;

namespace Iec61850Sim.Core.Biz.Model.Tree;

public interface IModelTreeBuilder
{
    IModelNode Build(string iedName, IedModel model, IPointRegistry registry);
}
