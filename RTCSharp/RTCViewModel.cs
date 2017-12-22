using OpenLibSys;
using System;
using System.Threading;

namespace RTCSharp
{
    public class RTCViewModel
    {
        private Ols ols;

        private const bool SMUSlow = false;
        private int SMUDelay = SMUSlow ? 60 : 10;

        public RTCViewModel()
        {
            ols = new Ols();

            Ols.OlsStatus status = ols.Status;

            Ols.OlsDllStatus dllStatus = ols.DllStatus;

            if (status != Ols.OlsStatus.NO_ERROR)
                throw new ApplicationException(string.Format("OlsStatus error:\nstatus {0}\ndllStatus {1}", status, dllStatus));

            if (dllStatus != Ols.OlsDllStatus.OLS_DLL_NO_ERROR)
                throw new ApplicationException(string.Format("OlsDllStatus error:\nstatus {0}\ndllStatus {1}", status, dllStatus));

            uint eax = 0, ebx = 0, ecx = 0, edx = 0;

            ols.CpuidPx(0x80000001, ref eax, ref ebx, ref ecx, ref edx, (UIntPtr)0x01);

            uint CPUFMS = eax & 0xFFFF00;

            if (CPUFMS != 0x00800F00 && CPUFMS != 0x00810F00)
            {
                ols.Dispose();
                throw new ApplicationException("The software only supports RV & ZP based Ryzen CPUs");
            }

            uint SMUORG = ols.ReadPciConfigDword(0x00, 0xB8);
            Thread.Sleep(SMUDelay);

            uint someOffset = ReadDword(0x50200) == 0x300 ? 0x100000u : 0x0u;

            // Read data
            uint BGS = ReadDword(0x00050058 + someOffset);
            uint BGSA = ReadDword(0x000500D0 + someOffset);

            uint DramConfiguration = ReadDword(0x00050200 + someOffset);

            uint DramTiming1 = ReadDword(0x00050204 + someOffset);
            uint DramTiming2 = ReadDword(0x00050208 + someOffset);
            uint DramTiming3 = ReadDword(0x0005020C + someOffset);
            uint DramTiming4 = ReadDword(0x00050210 + someOffset);
            uint DramTiming5 = ReadDword(0x00050214 + someOffset);
            uint DramTiming6 = ReadDword(0x00050218 + someOffset);
            uint DramTiming7 = ReadDword(0x0005021C + someOffset);
            uint DramTiming8 = ReadDword(0x00050220 + someOffset);
            uint DramTiming9 = ReadDword(0x00050224 + someOffset);
            uint DramTiming10 = ReadDword(0x00050228 + someOffset);
            // 11?
            uint DramTiming12 = ReadDword(0x00050230 + someOffset);
            uint DramTiming13 = ReadDword(0x00050234 + someOffset);
            uint DramTiming20 = ReadDword(0x00050250 + someOffset);
            uint DramTiming21 = ReadDword(0x00050254 + someOffset);
            uint DramTiming22 = ReadDword(0x00050258 + someOffset);

            uint tRFCTiming0 = ReadDword(0x00050260 + someOffset);
            uint tRFCTiming1 = ReadDword(0x00050264 + someOffset);

            uint tSTAGTiming0 = ReadDword(0x00050270 + someOffset);
            uint tSTAGTiming1 = ReadDword(0x00050274 + someOffset);

            uint DramTiming35 = ReadDword(0x0005028C + someOffset);

            uint tRFCTiming, tSTAGTiming;

            if (tRFCTiming0 == tRFCTiming1) // Checks if 2DPC is used or if tRFC has been manually set.
            {
                tRFCTiming = tRFCTiming0;
                tSTAGTiming = tSTAGTiming0;
            }
            else if (tRFCTiming0 == 0x21060138) // Checks if DIMM0 tRFC is set to AGESA defaults, meaning DIMM1 is populated.
            {
                tRFCTiming = tRFCTiming1;
                tSTAGTiming = tSTAGTiming1;
            }
            else
            {
                tRFCTiming = tRFCTiming0;
                tSTAGTiming = tSTAGTiming0;
            }

            this.BGS = BGS != 0x87654321;
            this.BGSA = BGSA == 0x111107F1;

            Preamble2T = (DramConfiguration & 0x1000) >> 12 != 0;
            GDM = (DramConfiguration & 0x800) >> 11 != 0;
            Cmd2T = (DramConfiguration & 0x400) >> 10 != 0;

            MEMCLK = DramConfiguration & 0x7F;
            float MEMCLKTRxx = MEMCLK / 3.0f * 100;
            MEMCLK = (uint)(MEMCLK / 3.0f * 200);

            tRCDWR = (DramTiming1 & 0x3F000000) >> 24;
            tRCDRD = (DramTiming1 & 0x3F0000) >> 16;
            tRAS = (DramTiming1 & 0x7F00) >> 8;
            tCL = DramTiming1 & 0x3F;

            tRPPB = (DramTiming2 & 0x3F000000) >> 24;
            tRP = (DramTiming2 & 0x3F0000) >> 16;
            tRCPB = (DramTiming2 & 0xFF00) >> 8;
            tRC = DramTiming2 & 0xFF;

            tRTP = (DramTiming3 & 0x1F000000) >> 24;
            tRRDDLR = (DramTiming3 & 0x1F0000) >> 16;
            tRRDL = (DramTiming3 & 0x1F00) >> 8;
            tRRDS = DramTiming3 & 0x1F;

            tFAWDLR = (DramTiming4 & 0x7E000000) >> 25;
            tFAWSLR = (DramTiming4 & 0xFC0000) >> 18;
            tFAW = DramTiming4 & 0x7F;

            tWTRL = (DramTiming5 & 0x7F0000) >> 16;
            tWTRS = (DramTiming5 & 0x1F00) >> 8;
            tCWL = DramTiming5 & 0x3F;

            tWR = DramTiming6 & 0x7F;

            tRCPage = (DramTiming7 & 0xFFF00000) >> 20;

            tRDRDBAN = (DramTiming8 & 0xC0000000) >> 30; // 0 - Disabled, 1 - Ban 2, 2 - Ban 2&3
            tRDRDSCL = (DramTiming8 & 0x3F000000) >> 24;
            tRDRDSCDLR = (DramTiming8 & 0xF00000) >> 20;
            tRDRDSC = (DramTiming8 & 0xF0000) >> 16;
            tRDRDSD = (DramTiming8 & 0xF00) >> 8;
            tRDRDDD = DramTiming8 & 0xF;

            tWRWRBAN = (DramTiming9 & 0xC0000000) >> 30; // 0 - Disabled, 1 - Ban 2, 2 - Ban 2&3
            tWRWRSCL = (DramTiming9 & 0x3F000000) >> 24;
            tWRWRSCDLR = (DramTiming9 & 0xF00000) >> 20;
            tWRWRSC = (DramTiming9 & 0xF0000) >> 16;
            tWRWRSD = (DramTiming9 & 0xF00) >> 8;
            tWRWRDD = DramTiming9 & 0xF;

            tWRRDSCDLR = (DramTiming10 & 0x1F0000) >> 16;
            tRDWR = (DramTiming10 & 0x1F00) >> 8;
            tWRRD = DramTiming10 & 0xF;

            tREF = DramTiming12 & 0xFFFF;
            tREFCT = (uint)(1000 / MEMCLKTRxx * tREF);

            tMODPDA = (DramTiming13 & 0x3F000000) >> 24;
            tMRDPDA = (DramTiming13 & 0x3F0000) >> 16;
            tMOD = (DramTiming13 & 0x3F00) >> 8;
            tMRD = DramTiming13 & 0x3F;

            tSTAG = (DramTiming20 & 0xFF0000) >> 16;

            tCKE = (DramTiming21 & 0x1F000000) >> 24;

            tPHYWRD = (DramTiming22 & 0x7000000) >> 24;
            tPHYRDLAT = (DramTiming22 & 0x3F0000) >> 16;
            tPHYWRLAT = (DramTiming22 & 0x1F00) >> 8;
            tRDDATA = DramTiming22 & 0x7F;

            tRFC4 = (tRFCTiming & 0xFFC00000) >> 22;
            tRFC4CT = (uint)(1000 / MEMCLKTRxx * tRFC4);

            tRFC2 = (tRFCTiming & 0x3FF800) >> 11;
            tRFC2CT = (uint)(1000 / MEMCLKTRxx * tRFC2);

            tRFC = tRFCTiming & 0x7FF;
            tRFCCT = (uint)(1000 / MEMCLKTRxx * tRFC);

            tSTAG4LR = (tSTAGTiming & 0x1FF00000) >> 20; // 0 - Disabled

            tSTAG2LR = (tSTAGTiming & 0x7FC00) >> 10; // 0 - Disabled

            tSTAGLR = tSTAGTiming & 0x1FF; // 0 - Disabled

            tWRMPR = (DramTiming35 & 0x3F000000) >> 24;

            ols.WritePciConfigDword(0x00, 0xB8, SMUORG);
            Thread.Sleep(SMUDelay);

            ols.Dispose();
        }

        private uint ReadDword(uint value)
        {
            ols.WritePciConfigDword(0x00, 0xB8, value);
            Thread.Sleep(SMUDelay);
            return ols.ReadPciConfigDword(0x00, 0xBC);
        }

        public bool BGS { get; private set; }
        public string BGS_Display => BGS ? "Enabled" : "Disabled";
        public bool BGSA { get; private set; }
        public string BGSA_Display => BGSA ? "Enabled" : "Disabled";
        public bool Preamble2T { get; private set; }
        public bool GDM { get; private set; }
        public string GDM_Display => GDM ? "Enabled" : "Disabled";
        public bool Cmd2T { get; private set; }
        public string CmdRate => Cmd2T ? "2T" : "1T";
        public uint MEMCLK { get; private set; }
        public uint tRCDWR { get; private set; }
        public uint tRCDRD { get; private set; }
        public uint tRAS { get; private set; }
        public uint tCL { get; private set; }
        public uint tRPPB { get; private set; }
        public uint tRP { get; private set; }
        public uint tRCPB { get; private set; }
        public uint tRC { get; private set; }
        public uint tRTP { get; private set; }
        public uint tRRDDLR { get; private set; }
        public uint tRRDL { get; private set; }
        public uint tRRDS { get; private set; }
        public uint tFAWDLR { get; private set; }
        public uint tFAWSLR { get; private set; }
        public uint tFAW { get; private set; }
        public uint tWTRL { get; private set; }
        public uint tWTRS { get; private set; }
        public uint tCWL { get; private set; }
        public uint tWR { get; private set; }
        public uint tRCPage { get; private set; }
        public uint tRDRDBAN { get; private set; }
        public string tRDRDBAN_Display => tRDRDBAN == 0 ? "Disabled" : (tRDRDBAN == 1 ? "Ban 2" : "Ban 2&3");
        public uint tRDRDSCL { get; private set; }
        public uint tRDRDSCDLR { get; private set; }
        public uint tRDRDSC { get; private set; }
        public uint tRDRDSD { get; private set; }
        public uint tRDRDDD { get; private set; }
        public uint tWRWRBAN { get; private set; }
        public string tWRWRBAN_Display => tWRWRBAN == 0 ? "Disabled" : (tWRWRBAN == 1 ? "Ban 2" : "Ban 2&3");
        public uint tWRWRSCL { get; private set; }
        public uint tWRWRSCDLR { get; private set; }
        public uint tWRWRSC { get; private set; }
        public uint tWRWRSD { get; private set; }
        public uint tWRWRDD { get; private set; }
        public uint tWRRDSCDLR { get; private set; }
        public uint tRDWR { get; private set; }
        public uint tWRRD { get; private set; }
        public uint tREF { get; private set; }
        public uint tREFCT { get; private set; }
        public uint tMODPDA { get; private set; }
        public uint tMRDPDA { get; private set; }
        public uint tMOD { get; private set; }
        public uint tMRD { get; private set; }
        public uint tSTAG { get; private set; }
        public uint tCKE { get; private set; }
        public uint tPHYWRD { get; private set; }
        public uint tPHYRDLAT { get; private set; }
        public uint tPHYWRLAT { get; private set; }
        public uint tRDDATA { get; private set; }
        public uint tRFC4 { get; private set; }
        public uint tRFC4CT { get; private set; }
        public uint tRFC2 { get; private set; }
        public uint tRFC2CT { get; private set; }
        public uint tRFC { get; private set; }
        public uint tRFCCT { get; private set; }
        public uint tSTAG4LR { get; private set; }
        public string tSTAG4LR_Display => tSTAG4LR == 0 ? "Disabled" : tSTAG4LR.ToString();
        public uint tSTAG2LR { get; private set; }
        public string tSTAG2LR_Display => tSTAG2LR == 0 ? "Disabled" : tSTAG2LR.ToString();
        public uint tSTAGLR { get; private set; }
        public string tSTAGLR_Display => tSTAGLR == 0 ? "Disabled" : tSTAGLR.ToString();
        public uint tWRMPR { get; private set; }
    }
}
