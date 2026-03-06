using IEC61850.Common;
using IEC61850.Server;
using Iec61850Sim.Models;
using Spectre.Console;
using System.Net;
using System.Runtime.InteropServices.ComTypes;
using static IEC61850.Server.IedServer;

namespace Iec61850Sim
{
    internal class Program
    {
        private static Iec61850ModelHelper _modelHelper = new Iec61850ModelHelper();

        static void Main(string[] args)
        {
            bool running = true;



            /* run until Ctrl-C is pressed */
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                running = false;
            };

            IedModel iedModel = ConfigFileParser.CreateModelFromConfigFile("Demo_Ed2.cfg");

            if (iedModel == null)
            {
                AnsiConsole.MarkupLine("[red]No valid data model found![/]");
                return;
            }

            iedModel.SetIedName("Demo");

            DataObject spcso1 = (DataObject)iedModel.GetModelNodeByShortObjectReference("Measurement/I3pMMXU1.A");

            IedServerConfig config = new IedServerConfig();
            config.ReportBufferSize = 100000;

            IedServer iedServer = new IedServer(iedModel, config);

            //COMANDOS
            iedServer.SetCheckHandler(spcso1, delegate (ControlAction action, object parameter, MmsValue ctlVal, bool test, bool interlockCheck)
            {

                AnsiConsole.MarkupLine("Received binary control command:");
                AnsiConsole.MarkupLine("   ctlNum: " + action.GetCtlNum());
                AnsiConsole.MarkupLine("   execution-time: " + action.GetControlTimeAsDataTimeOffset().ToString());

                return CheckHandlerResult.ACCEPTED;
            }, null);

            //Binary Control (SPC)
            iedServer.SetControlHandler(spcso1, delegate (ControlAction action, object parameter, MmsValue ctlVal, bool test)
            {
                bool val = ctlVal.GetBoolean();

                if (val)
                    AnsiConsole.MarkupLine("execute binary control command: [green]on[/]");
                else
                    AnsiConsole.MarkupLine("execute binary control command: [red]off[/]");

                return ControlHandlerResult.OK;
            }, null);

            DataObject spcso2 = (DataObject)iedModel.GetModelNodeByShortObjectReference("Measurement/U3pMMXU2.PhV");
            // SELECT / UNSELECT
            iedServer.SetSelectStateChangedHandler(spcso2, delegate (ControlAction action, object parameter, bool isSelected, SelectStateChangedReason reason)
            {
                DataObject cObj = action.GetControlObject();

                AnsiConsole.MarkupLine("Control object " + cObj.GetObjectReference() + (isSelected ? " selected" : " unselected") + " reason: " + reason.ToString());

            }, null);

            // REPORT
            iedServer.SetRCBEventHandler(delegate (object parameter, ReportControlBlock rcb, ClientConnection con, RCBEventType eventType, string parameterName, MmsDataAccessError serviceError)
            {
                AnsiConsole.MarkupLine("RCB: " + rcb.Parent.GetObjectReference() + "." + rcb.Name + " event: " + eventType.ToString());

                if (con != null)
                {
                    AnsiConsole.MarkupLine("  caused by client " + con.GetPeerAddress());
                }
                else
                {
                    AnsiConsole.MarkupLine("  client = null");
                }

                if (eventType == RCBEventType.ENABLED)
                {
                    AnsiConsole.MarkupLine("   RptID: " + rcb.RptID);
                    AnsiConsole.MarkupLine("   DatSet: " + rcb.DataSet);
                    AnsiConsole.MarkupLine("   TrgOps: " + rcb.TrgOps.ToString());
                }

                if ((eventType == RCBEventType.SET_PARAMETER) || (eventType == RCBEventType.GET_PARAMETER))
                {
                    AnsiConsole.MarkupLine("   param:  " + parameterName);
                    AnsiConsole.MarkupLine("   result: " + serviceError.ToString());
                }

            }, null);

            // Conexao de clientes
            void ConnectionCallBack(IedServer server, ClientConnection clientConnection, bool connected, object parameter)
            {
                if (connected)
                {
                    AnsiConsole.MarkupLine("Client [green]connected[/]: " + clientConnection.GetPeerAddress());
                }
                else
                {
                    AnsiConsole.MarkupLine("Client [red]disconnected[/]: " + clientConnection.GetPeerAddress());
                }
            }

            var connectionCallBack = new ConnectionIndicationHandler(ConnectionCallBack);

            iedServer.SetConnectionIndicationHandler(connectionCallBack, "127.0.0.1:102");


            // Inicializa server
            iedServer.Start(102);

            InitializeDigitalPoints(iedServer, iedModel);

            Random rand = new Random();

            if (iedServer.IsRunning())
            {
                AnsiConsole.MarkupLine("Server [green]Started[/] at port [yellow]102[/]!");

                GC.Collect();
                while (running)
                {
                    // Atualiza medições
                    UpdateMeasures(iedModel, iedServer, rand);
                }


                iedServer.Stop();
                AnsiConsole.MarkupLine("Server [red]stopped[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Failed to start server[/]");
            }

            iedServer.Destroy();
        }

        private static void UpdateMeasures(IedModel iedModel, IedServer iedServer, Random rand)
        {
            if (_modelHelper.AnalogicPoints.Count == 0)
                return;

            foreach (Analogic ana in _modelHelper.AnalogicPoints)
            {


                DataObject measurement = (DataObject)iedModel.GetModelNodeByShortObjectReference(ana.Address);
                DataAttribute measVal = (DataAttribute)measurement.GetChild("cVal.mag.f");
                DataAttribute measTs = (DataAttribute)measurement.GetChild("t");

                float floatVal = 1.0f;

                floatVal += (float)(rand.NextDouble() * 100);
                ana.Value = floatVal;
                ana.Timestamp = DateTime.Now;

                iedServer.UpdateTimestampAttributeValue(measTs, new Timestamp(ana.Timestamp));
                iedServer.UpdateFloatAttributeValue(measVal, floatVal);

                Thread.Sleep(100);
            }

            AnsiConsole.MarkupLine("Updated measurements at [yellow]" + DateTime.Now + "[/]");
            Thread.Sleep(5000);
        }

        private static void InitializeDigitalPoints(IedServer iedServer, IedModel iedModel)
        {
            if (_modelHelper.DigitalPoints.Count == 0)
                return;

            Random rand = new Random();
            foreach (Digital dig in _modelHelper.DigitalPoints)
            {

                DataObject measurement = (DataObject)iedModel.GetModelNodeByShortObjectReference(dig.Address);
                DataAttribute measVal = (DataAttribute)measurement.GetChild("stVal");
                DataAttribute measTs = (DataAttribute)measurement.GetChild("t");

                dig.Value = rand.Next(0, 4);
                dig.Timestamp = DateTime.Now;

                iedServer.UpdateTimestampAttributeValue(measTs, new Timestamp(dig.Timestamp));
                iedServer.UpdateInt32AttributeValue(measVal, dig.Value);
            }

            AnsiConsole.MarkupLine("Digital points initialized at [yellow]" + DateTime.Now + "[/]");
        }
    }

    internal class Iec61850ModelHelper
    {
        public Iec61850ModelHelper()
        {
            LoadVariables();
        }

        internal List<Analogic> AnalogicPoints { get; set; } = new List<Analogic>();

        internal List<Digital> DigitalPoints { get; set; } = new List<Digital>();

        private void LoadVariables() 
        {
            AnalogicPoints.Add(new Analogic("I3pMMXU1.A.phsA", "Measurement/I3pMMXU1.A.phsA"));
            AnalogicPoints.Add(new Analogic("I3pMMXU1.A.phsB", "Measurement/I3pMMXU1.A.phsB"));
            AnalogicPoints.Add(new Analogic("I3pMMXU1.A.phsC", "Measurement/I3pMMXU1.A.phsC"));            
            AnalogicPoints.Add(new Analogic("U3pMMXU2.PhV.phsA", "Measurement/U3pMMXU2.PhV.phsA"));
            AnalogicPoints.Add(new Analogic("U3pMMXU2.PhV.phsB", "Measurement/U3pMMXU2.PhV.phsB"));
            AnalogicPoints.Add(new Analogic("U3pMMXU2.PhV.phsC", "Measurement/U3pMMXU2.PhV.phsC"));

            DigitalPoints.Add(new Digital("Obj1CSWI1.Pos", "ProtCtrl/Obj1CSWI1.Pos"));
            DigitalPoints.Add(new Digital("Obj3CSWI2.Pos", "ProtCtrl/Obj3CSWI2.Pos"));
        }
        //private void LoadAllMeasurementAddresses()
        //{           
            
        //    //AnalogicPoints.Add(new Analogic("Measurement/I3pMMXU1.A.phsA"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/I3pMMXU1.A.phsB"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/I3pMMXU1.A.phsC"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/U3pMMXU1.PhV.phsA"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/U3pMMXU1.PhV.phsB"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/U3pMMXU1.PhV.phsC"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/U3pMMXU2.PhV.phsA"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/U3pMMXU2.PhV.phsB"));
        //    //AnalogicPoints.Add(new Analogic("Measurement/U3pMMXU2.PhV.phsC"));

        //    //DigitalPoints.Add(new Digital("ProtCtrl/Obj1CSWI1.Pos"));
        //    //DigitalPoints.Add(new Digital("ProtCtrl/Obj3CSWI2.Pos"));

        //    /*
        //    """"
        //    AI
        //    Measurement/I3pMMXU1.A.phsA $cVal$mag$f            
        //    Measurement/I3pMMXU1.A.phsB $cVal$mag$f
        //    Measurement/I3pMMXU1.A.phsC $cVal$mag$f
        //    Measurement/U3pMMXU1.PhV.phsA $cVal$mag$f
        //    Measurement/U3pMMXU1.PhV.phsB $cVal$mag$f
        //    Measurement/U3pMMXU1.PhV.phsC $cVal$mag$f
        //    Measurement/U3pMMXU2.PhV.phsA $cVal$mag$f
        //    Measurement/U3pMMXU2.PhV.phsB $cVal$mag$f
        //    Measurement/U3pMMXU2.PhV.phsC $cVal$mag$f
            
        //    BO 

        //    ProtCtrl/Obj1CSWI1$CO$Pos$Cancel$ctlNum:0
        //    ProtCtrl/Obj1CSWI1$CO$Pos$Oper$ctlVal

        //    ProtCtrl/Obj3CSWI2$CO$Pos$Cancel$ctlNum
        //    ProtCtrl/Obj3CSWI2$CO$Pos$Cancel$ctlVal
        //    ProtCtrl/Obj3CSWI2$CO$Pos$Oper$ctlNum
        //    ProtCtrl/Obj3CSWI2$CO$Pos$Oper$ctlVal
        //    ProtCtrl/Obj3CSWI2$CO$Pos$SBOw$ctlNum
        //    ProtCtrl/Obj3CSWI2$CO$Pos$SBOw$ctlVal
            
        //    BI

        //    ProtCtrl/Obj1CSWI1$ST$Pos$stVal
        //    ProtCtrl/Obj1CSWI1$ST$OpCntRs$stVal
        //    ProtCtrl/Obj1CSWI1$ST$Mod$stVal
        //    ProtCtrl/Obj1CSWI1$ST$Loc$stVal
        //    ProtCtrl/Obj1CSWI1$ST$Health$stVal
        //    ProtCtrl/Obj1CSWI1$ST$Beh$stVal

        //    ProtCtrl/Obj3CSWI2$ST$Pos$stVal
        //    ProtCtrl/Obj3CSWI2$ST$OpCntRs$stVal
        //    ProtCtrl/Obj3CSWI2$ST$Mod$stVal
        //    ProtCtrl/Obj3CSWI2$ST$Loc$stVal
        //    ProtCtrl/Obj3CSWI2$ST$Health$stVal
        //    ProtCtrl/Obj3CSWI2$ST$Beh$stVal


        //    ProtCtrl/Obj2XSWI1$ST$SwTyp$stVal
        //    ProtCtrl/Obj2XSWI1$ST$SwOpCap$stVal
        //    ProtCtrl/Obj2XSWI1$ST$Pos$stVal
        //    ProtCtrl/Obj2XSWI1$ST$OpCnt$stVal
        //    ProtCtrl/Obj2XSWI1$ST$Mod$stVal
        //    ProtCtrl/Obj2XSWI1$ST$Loc$stVal
        //    ProtCtrl/Obj2XSWI1$ST$Health$stVal
        //    ProtCtrl/Obj2XSWI1$ST$BlkOpn$stVal
        //    ProtCtrl/Obj2XSWI1$ST$BlkCls$stVal
        //    ProtCtrl/Obj2XSWI1$ST$Beh$stVal

            

        //    ProtCtrl/Obj3XCBR2$ST$Pos$stVal
        //    ProtCtrl/Obj3XCBR2$ST$OpCnt$stVal
        //    ProtCtrl/Obj3XCBR2$ST$Mod$stVal
        //    ProtCtrl/Obj3XCBR2$ST$Loc$stVal
        //    ProtCtrl/Obj3XCBR2$ST$Health$stVal
        //    ProtCtrl/Obj3XCBR2$ST$CBOpCap$stVal
        //    ProtCtrl/Obj3XCBR2$ST$BlkOpn$stVal
        //    ProtCtrl/Obj3XCBR2$ST$BlkCls$stVal
        //    ProtCtrl/Obj3XCBR2$ST$Beh$stVal

            
            
        //    ProtCtrl/Obj1XCBR1$ST$Pos$stVal
        //    ProtCtrl/Obj1XCBR1$ST$OpCnt$stVal
        //    ProtCtrl/Obj1XCBR1$ST$Mod$stVal
        //    ProtCtrl/Obj1XCBR1$ST$Loc$stVal
        //    ProtCtrl/Obj1XCBR1$ST$Health$stVal
        //    ProtCtrl/Obj1XCBR1$ST$CBOpCap$stVal
        //    ProtCtrl/Obj1XCBR1$ST$BlkOpn$stVal
        //    ProtCtrl/Obj1XCBR1$ST$BlkCls$stVal
        //    ProtCtrl/Obj1XCBR1$ST$Beh$stVal
            
        //    ProtCtrl/LPHD1$ST$Proxy$stVal
        //    ProtCtrl/LPHD1$ST$PhyHealth$stVal
        //    ProtCtrl/LLN0$ST$Mod$stVal
        //    ProtCtrl/LLN0$ST$Loc$stVal
        //    ProtCtrl/LLN0$ST$Health$stVal
        //    ProtCtrl/LLN0$ST$Beh$stVal
        //    ProtCtrl/I3GtPTRC1$ST$Tr$general
        //    ProtCtrl/I3GtPTRC1$ST$Mod$stVal
        //    ProtCtrl/I3GtPTRC1$ST$Health$stVal
        //    ProtCtrl/I3GtPTRC1$ST$Beh$stVal
        //    ProtCtrl/I3GtPTOC1$ST$Str$dirGeneral
        //    ProtCtrl/I3GtPTOC1$ST$Str$general
        //    ProtCtrl/I3GtPTOC1$ST$Op$general
        //    ProtCtrl/I3GtPTOC1$ST$Mod$stVal
        //    ProtCtrl/I3GtPTOC1$ST$Health$stVal
        //    ProtCtrl/I3GtPTOC1$ST$Beh$stVal
        //    ProtCtrl/DIGGIO1$ST$Mod$stVal
        //    ProtCtrl/DIGGIO1$ST$Ind5$stVal
        //    ProtCtrl/DIGGIO1$ST$Ind4$stVal
        //    ProtCtrl/DIGGIO1$ST$Ind3$stVal
        //    ProtCtrl/DIGGIO1$ST$Ind2$stVal
        //    ProtCtrl/DIGGIO1$ST$Ind1$stVal
        //    ProtCtrl/DIGGIO1$ST$Health$stVal
        //    ProtCtrl/DIGGIO1$ST$Beh$stVal
        //    Measurement/U3pMMXU2$ST$Mod$stVal
        //    Measurement/U3pMMXU2$ST$Health$stVal
        //    Measurement/U3pMMXU2$ST$Beh$stVal
        //    Measurement/I3pMMXU1$ST$Mod$stVal
        //    Measurement/I3pMMXU1$ST$Health$stVal
        //    Measurement/I3pMMXU1$ST$Beh$stVal

        //    REPORTS
        //    ProtCtrl/LLN0$RP$urcb02$DemoProtCtrl/LLN0$RP$urcb02
        //    ProtCtrl/LLN0$RP$urcb01$DemoProtCtrl/LLN0$RP$urcb01
        //    Measurement/LLN0$BR$brcb02
        //    Measurement/LLN0$BR$brcb01
            
        //    """"*/

        //    //return result;
        //}
    }
}
