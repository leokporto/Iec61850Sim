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

            host.Publish([floatPoint, intPoint, boolPoint]);

            Assert.Equal(12.5f, host.Server.GetAttributeValue(context.FloatValueAttribute).ToFloat(), 3);
            Assert.Equal(7, host.Server.GetAttributeValue(context.IntValueAttribute).ToInt32());
            var boolValue = host.Server.GetAttributeValue(context.BoolValueAttribute);
            Assert.NotNull(boolValue);
            Assert.True(boolValue.GetBoolean());
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void Publish_WithCodeDenumInteger_ShouldUpdateAsBitString()
    {
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var point = CreatePoint("LD0/XCBR1.Pos.stVal", "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute);
            point.Value = 2;

            host.Publish([point]);
            var value = host.Server.GetAttributeValue(context.CodedEnumAttribute);

            Assert.Equal(MmsType.MMS_BIT_STRING, value.GetType());
            Assert.Equal(2u, value.BitStringToUInt32BigEndian());
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void Publish_WithNullValueOrNullValueAttribute_ShouldSkipInvalidPoints()
    {
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

            host.Publish([valid, nullValue, nullAttribute]);

            Assert.Equal(99, host.Server.GetAttributeValue(context.IntValueAttribute).ToInt32());
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void Publish_WithTimestampAndQuality_ShouldUpdateAuxiliaryAttributes()
    {
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

            host.Publish([point]);

            var qualityValue = host.Server.GetAttributeValue(context.QualityAttribute).BitStringToUInt32BigEndian();
            var timestampValue = host.Server.GetAttributeValue(context.TimestampAttribute).GetUtcTimeAsDateTimeOffset();

            Assert.NotEqual(qualityBefore, qualityValue);
            Assert.True(timestampValue > DateTimeOffset.UnixEpoch);
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void ControlHandler_WithOnCommand_ShouldOperateAndReturnOk()
    {
        var context = CreateContext();
        var host = context.Host;
        try
        {
            const string breakerRef = "LD0/XCBR1.Pos.stVal";
            const string cswiPosRef = "LD0/CSWI1.Pos.stVal";
            const string cswiCmdRef = "LD0/CSWI1.Pos.Oper.ctlVal";

            var breakerPos = CreatePoint(breakerRef, "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var controllerPos = CreatePoint(cswiPosRef, "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var cmdPoint = CreatePoint(cswiCmdRef, "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.CO, context.BoolValueAttribute, equipmentName: "Q01");

            context.Registry.Register(breakerPos);
            context.Registry.Register(controllerPos);
            context.Registry.Register(cmdPoint);
            context.Registry.SetValue(new PointValue<object> { Reference = breakerRef, Value = 1, Timestamp = DateTimeOffset.UtcNow, Quality = 192 });

            var breaker = new Breaker("XCBR1", breakerRef, "Q01", context.Registry);
            var controller = new Cswi("CSWI1", cswiCmdRef, cswiPosRef, breaker, context.Registry);

            var method = typeof(IecServerHost).GetMethod("ControlHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            var result = (ControlHandlerResult)method!.Invoke(host, [action, controller, new MmsValue(true), false])!;

            Assert.Equal(ControlHandlerResult.OK, result);
            Assert.Equal(2, context.Registry.GetValue<object>(breakerRef).Value);
            Assert.Equal(2, context.Registry.GetValue<object>(cswiPosRef).Value);
            Assert.Equal(2u, host.Server.GetAttributeValue(context.CodedEnumAttribute).BitStringToUInt32BigEndian());
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void CheckHandler_WhenInvoked_ShouldReturnAccepted()
    {
        var context = CreateContext();
        var host = context.Host;
        try
        {
            var method = typeof(IecServerHost).GetMethod("CheckHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            var result = (CheckHandlerResult)method!.Invoke(host, [action, new object(), new MmsValue(true), false, false])!;

            Assert.Equal(CheckHandlerResult.ACCEPTED, result);
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void SelectStateChanged_WithoutPendingCtlVal_ShouldNotThrow()
    {
        var context = CreateContext();
        var host = context.Host;
        try
        {
            const string breakerRef = "LD0/XCBR1.Pos.stVal";
            const string cswiPosRef = "LD0/CSWI1.Pos.stVal";
            const string cswiCmdRef = "LD0/CSWI1.Pos.Oper.ctlVal";

            var breakerPos = CreatePoint(breakerRef, "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var controllerPos = CreatePoint(cswiPosRef, "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var cmdPoint = CreatePoint(cswiCmdRef, "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.CO, context.BoolValueAttribute, equipmentName: "Q01");

            context.Registry.Register(breakerPos);
            context.Registry.Register(controllerPos);
            context.Registry.Register(cmdPoint);

            var breaker = new Breaker("XCBR1", breakerRef, "Q01", context.Registry);
            var controller = new Cswi("CSWI1", cswiCmdRef, cswiPosRef, breaker, context.Registry);

            var method = typeof(IecServerHost).GetMethod("SelectStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            var exception = Record.Exception(() =>
                method!.Invoke(host, [action, controller, true, SelectStateChangedReason.SELECT_STATE_REASON_SELECTED]));

            Assert.Null(exception);
        }
        finally { host.Stop(); }
    }

    [Fact]
    public void SelectStateChanged_AfterCheckHandlerStoresCtlVal_ShouldOperateAndPublish()
    {
        var context = CreateContext();
        var host = context.Host;
        try
        {
            const string breakerRef = "LD0/XCBR1.Pos.stVal";
            const string cswiPosRef = "LD0/CSWI1.Pos.stVal";
            const string cswiCmdRef = "LD0/CSWI1.Pos.SBOw.ctlVal";

            var breakerPos = CreatePoint(breakerRef, "XCBR1", eLnType.XCBR, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var controllerPos = CreatePoint(cswiPosRef, "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.ST, context.CodedEnumAttribute, equipmentName: "Q01");
            var cmdPoint = CreatePoint(cswiCmdRef, "CSWI1", eLnType.CSWI, "Pos", FunctionalConstraint.CO, context.BoolValueAttribute, equipmentName: "Q01");

            context.Registry.Register(breakerPos);
            context.Registry.Register(controllerPos);
            context.Registry.Register(cmdPoint);
            context.Registry.SetValue(new PointValue<object> { Reference = breakerRef, Value = (int)eDblPos.Off, Timestamp = DateTimeOffset.UtcNow, Quality = 192 });

            var breaker = new Breaker("XCBR1", breakerRef, "Q01", context.Registry);
            var controller = new Cswi("CSWI1", cswiCmdRef, cswiPosRef, breaker, context.Registry);

            var selectMethod = typeof(IecServerHost).GetMethod("SelectStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(selectMethod);

            // Simulate CheckHandler having stored ctlVal for this controller
            var pendingField = typeof(IecServerHost).GetField("_pendingSelectCtlVals", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(pendingField);
            var pending = (Dictionary<string, MmsValue>)pendingField!.GetValue(host)!;
            pending[controller.Name] = new MmsValue(true); // true → Close

            var action = (ControlAction)RuntimeHelpers.GetUninitializedObject(typeof(ControlAction));

            selectMethod!.Invoke(host, [action, controller, true, SelectStateChangedReason.SELECT_STATE_REASON_SELECTED]);

            Assert.Equal((int)eDblPos.On, context.Registry.GetValue<object>(breakerRef).Value);
            Assert.Equal((int)eDblPos.On, context.Registry.GetValue<object>(cswiPosRef).Value);
            Assert.Equal(2u, host.Server.GetAttributeValue(context.CodedEnumAttribute).BitStringToUInt32BigEndian());
        }
        finally { host.Stop(); }
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
        var commandProcessor = new ControlCommandProcessor(registry);

        var host = new IecServerHost(model, registry, manager, commandProcessor);

        return new HostContext(host, registry, floatDa, intDa, boolDa, qualityDa, tsDa, codedEnumDa);
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
        PointRegistry Registry,
        DataAttribute FloatValueAttribute,
        DataAttribute IntValueAttribute,
        DataAttribute BoolValueAttribute,
        DataAttribute QualityAttribute,
        DataAttribute TimestampAttribute,
        DataAttribute CodedEnumAttribute);
}
