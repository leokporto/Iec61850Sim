using System.Reflection;
using System.Runtime.CompilerServices;
using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Core.Biz.Commands;
using Iec61850Sim.Core.Biz.Device;
using Iec61850Sim.Core.Biz.Points;
using Iec61850Sim.Core.Iec61850;
using Iec61850Sim.Core.Model;
using Iec61850Sim.Core.Model.Devices;
using Xunit;

namespace Iec61850Sim.UnitTests.Iec61850;

public class IecServerHostTests
{
    [Fact]
    public void Publish_WithPrimitiveValues_ShouldUpdateServerAttributes()
    {
        // Arrange
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var floatPoint = CreatePoint("LD0/MMXU1.phsA.mag.f", "MMXU1", eLnType.MMXU, "PhV", FunctionalConstraint.MX, context.FloatValueAttribute);
            var intPoint = CreatePoint("LD0/GGIO1.Ind.stVal", "GGIO1", eLnType.GGIO, "Ind", FunctionalConstraint.ST, context.IntValueAttribute);
            var boolPoint = CreatePoint("LD0/GGIO1.SPCSO.ctlVal", "GGIO1", eLnType.GGIO, "SPCSO", FunctionalConstraint.ST, context.BoolValueAttribute);

            floatPoint.Value = 12.5d;
            intPoint.Value = 7;
            boolPoint.Value = true;

            // Act
            host.Publish([floatPoint, intPoint, boolPoint]);

            // Assert
            Assert.Equal(12.5f, host.Server.GetAttributeValue(context.FloatValueAttribute).ToFloat(), 3);
            Assert.Equal(7, host.Server.GetAttributeValue(context.IntValueAttribute).ToInt32());
            var boolValue = host.Server.GetAttributeValue(context.BoolValueAttribute);
            Assert.NotNull(boolValue);
            Assert.True(boolValue.GetBoolean());
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void Publish_WithCodeDenumInteger_ShouldUpdateAsBitString()
    {
        // Arrange
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var point = CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute);
            point.Value = 2;

            // Act
            host.Publish([point]);
            var value = host.Server.GetAttributeValue(context.CodedEnumAttribute);

            // Assert
            Assert.Equal(MmsType.MMS_BIT_STRING, value.GetType());
            Assert.Equal(2u, value.BitStringToUInt32BigEndian());
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void Publish_WithNullValueOrNullValueAttribute_ShouldSkipInvalidPoints()
    {
        // Arrange
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var valid = CreatePoint("LD0/GGIO1.Ind.stVal", "GGIO1", eLnType.GGIO, "Ind", FunctionalConstraint.ST, context.IntValueAttribute);
            valid.Value = 99;

            var nullValue = CreatePoint("LD0/MMXU1.phsA.mag.f", "MMXU1", eLnType.MMXU, "PhV", FunctionalConstraint.MX, context.FloatValueAttribute);
            nullValue.Value = null;

            var nullAttribute = CreatePoint("LD0/NOATTR.stVal", "GGIO2", eLnType.GGIO, "Ind", FunctionalConstraint.ST, context.IntValueAttribute);
            nullAttribute.ValueAttribute = null!;
            nullAttribute.Value = 10;

            // Act
            host.Publish([valid, nullValue, nullAttribute]);

            // Assert
            Assert.Equal(99, host.Server.GetAttributeValue(context.IntValueAttribute).ToInt32());
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void Publish_WithTimestampAndQuality_ShouldUpdateAuxiliaryAttributes()
    {
        // Arrange
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var point = CreatePoint("LD0/GGIO1.Ind.stVal", "GGIO1", eLnType.GGIO, "Ind", FunctionalConstraint.ST, context.IntValueAttribute);
            point.Value = 5;
            point.Timestamp = DateTimeOffset.UtcNow;
            point.Quality = 123;
            point.TimestampAttribute = context.TimestampAttribute;
            point.QualityAttribute = context.QualityAttribute;
            var qualityBefore = host.Server.GetAttributeValue(context.QualityAttribute).BitStringToUInt32BigEndian();

            // Act
            host.Publish([point]);

            // Assert
            var qualityValue = host.Server.GetAttributeValue(context.QualityAttribute).BitStringToUInt32BigEndian();
            var timestampValue = host.Server.GetAttributeValue(context.TimestampAttribute).GetUtcTimeAsDateTimeOffset();

            Assert.NotEqual(qualityBefore, qualityValue);
            Assert.True(timestampValue > DateTimeOffset.UnixEpoch);
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void ControlHandler_WithOnCommand_ShouldOperateAndReturnOk()
    {
        // Arrange
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var breakerPos = CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            breakerPos.Value = 1;

            var controllerPos = CreatePoint("LD0/CSWI1.Pos.stVal", "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var cmdPoint = CreatePoint("LD0/CSWI1.Pos.Oper.ctlVal", "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.CO, context.BoolValueAttribute, equipmentName: "Q01");

            var breaker = new Breaker("XCBR1", breakerPos);
            var controller = new Cswi("CSWI1", cmdPoint, controllerPos, breaker);

            var method = typeof(IecServerHost).GetMethod("ControlHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            // Act
            var result = (ControlHandlerResult)method!.Invoke(host, [action, controller, new MmsValue(true), false])!;

            // Assert
            Assert.Equal(ControlHandlerResult.OK, result);
            Assert.Equal(2, breaker.Position.Value);
            Assert.Equal(2, controller.Position.Value);
            Assert.Equal(2u, host.Server.GetAttributeValue(context.CodedEnumAttribute).BitStringToUInt32BigEndian());
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void CheckHandler_WhenInvoked_ShouldReturnAccepted()
    {
        // Arrange
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var method = typeof(IecServerHost).GetMethod("CheckHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            // Act
            var result = (CheckHandlerResult)method!.Invoke(host, [action, new object(), new MmsValue(true), false, false])!;

            // Assert
            Assert.Equal(CheckHandlerResult.ACCEPTED, result);
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void SelectStateChanged_WithoutPendingCtlVal_ShouldNotThrow()
    {
        // Arrange — sem ctlVal pendente no dicionário: TryGetValue retorna false → retorno antecipado
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var breakerPos = CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var controllerPos = CreatePoint("LD0/CSWI1.Pos.stVal", "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var cmdPoint = CreatePoint("LD0/CSWI1.Pos.Oper.ctlVal", "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.CO, context.BoolValueAttribute, equipmentName: "Q01");

            var controller = new Cswi("CSWI1", cmdPoint, controllerPos, new Breaker("XCBR1", breakerPos));
            var method = typeof(IecServerHost).GetMethod("SelectStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            // Act
            var exception = Record.Exception(() =>
                method!.Invoke(host, [action, controller, true, SelectStateChangedReason.SELECT_STATE_REASON_SELECTED]));

            // Assert
            Assert.Null(exception);
        }
        finally
        {
            host.Stop();
        }
    }

    [Fact]
    public void SelectStateChanged_AfterCheckHandlerStoresCtlVal_ShouldOperateAndPublish()
    {
        // Arrange — simula o fluxo SBO: CheckHandler (SELECT) → SelectStateChanged
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var breakerPos = CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            breakerPos.Value = (int)eDblPos.Off;

            var controllerPos = CreatePoint("LD0/CSWI1.Pos.stVal", "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var cmdPoint = CreatePoint("LD0/CSWI1.Pos.SBOw.ctlVal", "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.CO, context.BoolValueAttribute, equipmentName: "Q01");

            var breaker = new Breaker("XCBR1", breakerPos);
            var controller = new Cswi("CSWI1", cmdPoint, controllerPos, breaker);

            var checkMethod = typeof(IecServerHost).GetMethod("CheckHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            var selectMethod = typeof(IecServerHost).GetMethod("SelectStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(checkMethod);
            Assert.NotNull(selectMethod);

            // CheckHandler é chamado na fase SELECT com action.IsSelect()=true
            // Como ControlAction é uma classe nativa sem construtor público,
            // usamos o dicionário diretamente via reflection para simular o armazenamento do ctlVal
            var pendingField = typeof(IecServerHost).GetField("_pendingSelectCtlVals", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(pendingField);
            var pending = (Dictionary<string, MmsValue>)pendingField!.GetValue(host)!;
            pending[controller.Name] = new MmsValue(true); // ctlVal = true → Close

            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            // Act
            selectMethod!.Invoke(host, [action, controller, true, SelectStateChangedReason.SELECT_STATE_REASON_SELECTED]);

            // Assert — breaker deve ter fechado (On = 2)
            Assert.Equal((int)eDblPos.On, breaker.Position.Value);
            Assert.Equal((int)eDblPos.On, controller.Position.Value);
            Assert.Equal(2u, host.Server.GetAttributeValue(context.CodedEnumAttribute).BitStringToUInt32BigEndian());
        }
        finally
        {
            host.Stop();
        }
    }

    private static HostContext CreateContext()
    {
        var model = new IedModel("IED_TEST");
        var ld = new LogicalDevice("LD0", model);

        var mmxu = new LogicalNode("MMXU1", ld);
        var phv = new DataObject("PhV", mmxu);
        var floatDa = new DataAttribute("f", phv, DataAttributeType.FLOAT32, FunctionalConstraint.MX, TriggerOptions.NONE, 0);

        var ggio = new LogicalNode("GGIO1", ld);
        var ind = new DataObject("Ind", ggio);
        var intDa = new DataAttribute("stVal", ind, DataAttributeType.INT32, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var boolDa = new DataAttribute("ctlVal", ind, DataAttributeType.BOOLEAN, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var qualityDa = new DataAttribute("q", ind, DataAttributeType.QUALITY, FunctionalConstraint.ST, TriggerOptions.NONE, 0);
        var tsDa = new DataAttribute("t", ind, DataAttributeType.TIMESTAMP, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var xcbr = new LogicalNode("XCBR1", ld);
        var pos = new DataObject("Pos", xcbr);
        var codedEnumDa = new DataAttribute("stVal", pos, DataAttributeType.CODEDENUM, FunctionalConstraint.ST, TriggerOptions.NONE, 0);

        var registry = new PointRegistry();
        var manager = new DeviceManager();
        var commandProcessor = new ControlCommandProcessor();
        var host = new IecServerHost(model, registry, manager, commandProcessor);

        return new HostContext(host, floatDa, intDa, boolDa, qualityDa, tsDa, codedEnumDa);
    }

    private static DevicePoint CreatePoint(
        string reference,
        string logicalNode,
        eLnType lnType,
        string dataObject,
        FunctionalConstraint fc,
        DataAttribute valueAttribute,
        string equipmentName = "EQ")
    {
        return new DevicePoint
        {
            Reference = reference,
            EquipmentName = equipmentName,
            LogicalDevice = "LD0",
            LogicalNode = logicalNode,
            DataObject = dataObject,
            LnType = lnType,
            Fc = fc,
            ValueAttribute = valueAttribute
        };
    }

    private sealed record HostContext(
        IecServerHost Host,
        DataAttribute FloatValueAttribute,
        DataAttribute IntValueAttribute,
        DataAttribute BoolValueAttribute,
        DataAttribute QualityAttribute,
        DataAttribute TimestampAttribute,
        DataAttribute CodedEnumAttribute);
}
