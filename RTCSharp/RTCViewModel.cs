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

        public unsafe RTCViewModel()
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

            if (CPUFMS != 0x00800F00 && CPUFMS != 0x00810F00 && CPUFMS != 0x00870F00)
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

            ols.WritePciConfigDword(0x00, 0xB8, 0x3B10528);
            ols.WritePciConfigDword(0x00, 0xBC, 0x02);
            ols.WritePciConfigDword(0x00, 0xB8, 0x3B10598);
            uint somethingVersion = ols.ReadPciConfigDword(0, 0xBC); // some agesa version? 0x195200, 0x195300...

            uint eax2 = 0, ebx2 = 0, ecx2 = 0, edx2 = 0;
            ols.CpuidPx(0x80000001, ref eax2, ref ebx2, ref ecx2, ref edx2, (UIntPtr)0x01);
            eax2 &= 0xFFFF00;
            ebx2 = (ebx2 & 0xF0000000) >> 28;

            uint someOffset2 = 0;

            if (ebx2 == 7)
                someOffset2 = 0x2180;
            else if (ebx2 == 2)
                someOffset2 = 0x100;
            else
                someOffset2 = 0x00;

            if (eax2 == 0x810F00) // Raven Ridge?
            {
                this.ProcODT = "N/A";
                this.RttNom = "N/A";
                this.RttWr = "N/A";
                this.RttPark = "N/A";
                this.AddrCmdSetup = "N/A";
                this.CsOdtSetup = "N/A";
                this.CkeSetup = "N/A";
                this.ClkDrvStrength = "N/A";
                this.AddrCmdDrvStrength = "N/A";
                this.CsOdtDrvStrength = "N/A";
                this.CkeDrvStrength = "N/A";
            }
            else if (false/*eax2 == 0x800F00 && somethingVersion < 0x195300*/) // Zeppelin? & some version < X
            {
                this.ProcODT = "N/A";
                this.RttNom = "N/A";
                this.RttWr = "N/A";
                this.RttPark = "N/A";
                this.AddrCmdSetup = "N/A";
                this.CsOdtSetup = "N/A";
                this.CkeSetup = "N/A";
                this.ClkDrvStrength = "N/A";
                this.AddrCmdDrvStrength = "N/A";
                this.CsOdtDrvStrength = "N/A";
                this.CkeDrvStrength = "N/A";
            }
            else if (ebx2 == 1 || ebx2 == 3 || ebx2 == 4)
            {
                this.ProcODT = "N/A";
                this.RttNom = "N/A";
                this.RttWr = "N/A";
                this.RttPark = "N/A";
                this.AddrCmdSetup = "N/A";
                this.CsOdtSetup = "N/A";
                this.CkeSetup = "N/A";
                this.ClkDrvStrength = "N/A";
                this.AddrCmdDrvStrength = "N/A";
                this.CsOdtDrvStrength = "N/A";
                this.CkeDrvStrength = "N/A";
            }
            else
            {
                ols.WritePciConfigDword(0x00, 0xB8, 0x3B10528);
                ols.WritePciConfigDword(0x00, 0xBC, 0x2C);
                //ols.WritePciConfigDword(0x00, 0xBC, 0x25);
                ols.WritePciConfigDword(0x00, 0xB8, 0x3B1059C);
                uint x = ols.ReadPciConfigDword(0, 0xBC);
                ulong num26 = x - someOffset2;

                Ols.IsInpOutDriverOpen2();

                //uint num27 = 0xB1;
                //uint physLong1 = Ols.GetPhysLong2(new UIntPtr(num26 + num27));
                //uint num28 = 0xB5;
                //uint physLong2 = Ols.GetPhysLong2(new UIntPtr(num26 + num28));
                //uint num29 = 0xBA;
                //uint physLong3 = Ols.GetPhysLong2(new UIntPtr(num26 + num29));

                uint num27 = 0xB1;
                uint physLong1 = ols.GetPhysLong(new UIntPtr(num26 + num27));
                uint num28 = 0xB5;
                uint physLong2 = ols.GetPhysLong(new UIntPtr(num26 + num28));
                uint num29 = 0xBA;
                uint physLong3 = ols.GetPhysLong(new UIntPtr(num26 + num29));

                uint addrCmdSetup = physLong1 & 0xFF;
                // addrCmdSetup / 32, addrCmdSetup % 32
                switch (addrCmdSetup)
                {
                    case 0:
                        this.AddrCmdSetup = "0/0";
                        break;
                    case 1:
                        this.AddrCmdSetup = "0/1";
                        break;
                    case 2:
                        this.AddrCmdSetup = "0/2";
                        break;
                    case 3:
                        this.AddrCmdSetup = "0/3";
                        break;
                    case 4:
                        this.AddrCmdSetup = "0/4";
                        break;
                    case 5:
                        this.AddrCmdSetup = "0/5";
                        break;
                    case 6:
                        this.AddrCmdSetup = "0/6";
                        break;
                    case 7:
                        this.AddrCmdSetup = "0/7";
                        break;
                    case 8:
                        this.AddrCmdSetup = "0/8";
                        break;
                    case 9:
                        this.AddrCmdSetup = "0/9";
                        break;
                    case 10:
                        this.AddrCmdSetup = "0/10";
                        break;
                    case 11:
                        this.AddrCmdSetup = "0/11";
                        break;
                    case 12:
                        this.AddrCmdSetup = "0/12";
                        break;
                    case 13:
                        this.AddrCmdSetup = "0/13";
                        break;
                    case 14:
                        this.AddrCmdSetup = "0/14";
                        break;
                    case 15:
                        this.AddrCmdSetup = "0/15";
                        break;
                    case 16:
                        this.AddrCmdSetup = "0/16";
                        break;
                    case 17:
                        this.AddrCmdSetup = "0/17";
                        break;
                    case 18:
                        this.AddrCmdSetup = "0/18";
                        break;
                    case 19:
                        this.AddrCmdSetup = "0/19";
                        break;
                    case 20:
                        this.AddrCmdSetup = "0/20";
                        break;
                    case 21:
                        this.AddrCmdSetup = "0/21";
                        break;
                    case 22:
                        this.AddrCmdSetup = "0/22";
                        break;
                    case 23:
                        this.AddrCmdSetup = "0/23";
                        break;
                    case 24:
                        this.AddrCmdSetup = "0/24";
                        break;
                    case 25:
                        this.AddrCmdSetup = "0/25";
                        break;
                    case 26:
                        this.AddrCmdSetup = "0/26";
                        break;
                    case 27:
                        this.AddrCmdSetup = "0/27";
                        break;
                    case 28:
                        this.AddrCmdSetup = "0/28";
                        break;
                    case 29:
                        this.AddrCmdSetup = "0/29";
                        break;
                    case 30:
                        this.AddrCmdSetup = "0/30";
                        break;
                    case 31:
                        this.AddrCmdSetup = "0/31";
                        break;
                    case 32:
                        this.AddrCmdSetup = "1/0";
                        break;
                    case 33:
                        this.AddrCmdSetup = "1/1";
                        break;
                    case 34:
                        this.AddrCmdSetup = "1/2";
                        break;
                    case 35:
                        this.AddrCmdSetup = "1/3";
                        break;
                    case 36:
                        this.AddrCmdSetup = "1/4";
                        break;
                    case 37:
                        this.AddrCmdSetup = "1/5";
                        break;
                    case 38:
                        this.AddrCmdSetup = "1/6";
                        break;
                    case 39:
                        this.AddrCmdSetup = "1/7";
                        break;
                    case 40:
                        this.AddrCmdSetup = "1/8";
                        break;
                    case 41:
                        this.AddrCmdSetup = "1/9";
                        break;
                    case 42:
                        this.AddrCmdSetup = "1/10";
                        break;
                    case 43:
                        this.AddrCmdSetup = "1/11";
                        break;
                    case 44:
                        this.AddrCmdSetup = "1/12";
                        break;
                    case 45:
                        this.AddrCmdSetup = "1/13";
                        break;
                    case 46:
                        this.AddrCmdSetup = "1/14";
                        break;
                    case 47:
                        this.AddrCmdSetup = "1/15";
                        break;
                    case 48:
                        this.AddrCmdSetup = "1/16";
                        break;
                    case 49:
                        this.AddrCmdSetup = "1/17";
                        break;
                    case 50:
                        this.AddrCmdSetup = "1/18";
                        break;
                    case 51:
                        this.AddrCmdSetup = "1/19";
                        break;
                    case 52:
                        this.AddrCmdSetup = "1/20";
                        break;
                    case 53:
                        this.AddrCmdSetup = "1/21";
                        break;
                    case 54:
                        this.AddrCmdSetup = "1/22";
                        break;
                    case 55:
                        this.AddrCmdSetup = "1/23";
                        break;
                    case 56:
                        this.AddrCmdSetup = "1/24";
                        break;
                    case 57:
                        this.AddrCmdSetup = "1/25";
                        break;
                    case 58:
                        this.AddrCmdSetup = "1/26";
                        break;
                    case 59:
                        this.AddrCmdSetup = "1/27";
                        break;
                    case 60:
                        this.AddrCmdSetup = "1/28";
                        break;
                    case 61:
                        this.AddrCmdSetup = "1/29";
                        break;
                    case 62:
                        this.AddrCmdSetup = "1/30";
                        break;
                    case 63:
                        this.AddrCmdSetup = "1/31";
                        break;
                }

                uint csOdtSetup = (physLong1 & 0xFF00) >> 8;
                // csOdtSetup / 32, csOdtSetup % 32
                switch (csOdtSetup)
                {
                    case 0:
                        this.CsOdtSetup = "0/0";
                        break;
                    case 1:
                        this.CsOdtSetup = "0/1";
                        break;
                    case 2:
                        this.CsOdtSetup = "0/2";
                        break;
                    case 3:
                        this.CsOdtSetup = "0/3";
                        break;
                    case 4:
                        this.CsOdtSetup = "0/4";
                        break;
                    case 5:
                        this.CsOdtSetup = "0/5";
                        break;
                    case 6:
                        this.CsOdtSetup = "0/6";
                        break;
                    case 7:
                        this.CsOdtSetup = "0/7";
                        break;
                    case 8:
                        this.CsOdtSetup = "0/8";
                        break;
                    case 9:
                        this.CsOdtSetup = "0/9";
                        break;
                    case 10:
                        this.CsOdtSetup = "0/10";
                        break;
                    case 11:
                        this.CsOdtSetup = "0/11";
                        break;
                    case 12:
                        this.CsOdtSetup = "0/12";
                        break;
                    case 13:
                        this.CsOdtSetup = "0/13";
                        break;
                    case 14:
                        this.CsOdtSetup = "0/14";
                        break;
                    case 15:
                        this.CsOdtSetup = "0/15";
                        break;
                    case 16:
                        this.CsOdtSetup = "0/16";
                        break;
                    case 17:
                        this.CsOdtSetup = "0/17";
                        break;
                    case 18:
                        this.CsOdtSetup = "0/18";
                        break;
                    case 19:
                        this.CsOdtSetup = "0/19";
                        break;
                    case 20:
                        this.CsOdtSetup = "0/20";
                        break;
                    case 21:
                        this.CsOdtSetup = "0/21";
                        break;
                    case 22:
                        this.CsOdtSetup = "0/22";
                        break;
                    case 23:
                        this.CsOdtSetup = "0/23";
                        break;
                    case 24:
                        this.CsOdtSetup = "0/24";
                        break;
                    case 25:
                        this.CsOdtSetup = "0/25";
                        break;
                    case 26:
                        this.CsOdtSetup = "0/26";
                        break;
                    case 27:
                        this.CsOdtSetup = "0/27";
                        break;
                    case 28:
                        this.CsOdtSetup = "0/28";
                        break;
                    case 29:
                        this.CsOdtSetup = "0/29";
                        break;
                    case 30:
                        this.CsOdtSetup = "0/30";
                        break;
                    case 31:
                        this.CsOdtSetup = "0/31";
                        break;
                    case 32:
                        this.CsOdtSetup = "1/0";
                        break;
                    case 33:
                        this.CsOdtSetup = "1/1";
                        break;
                    case 34:
                        this.CsOdtSetup = "1/2";
                        break;
                    case 35:
                        this.CsOdtSetup = "1/3";
                        break;
                    case 36:
                        this.CsOdtSetup = "1/4";
                        break;
                    case 37:
                        this.CsOdtSetup = "1/5";
                        break;
                    case 38:
                        this.CsOdtSetup = "1/6";
                        break;
                    case 39:
                        this.CsOdtSetup = "1/7";
                        break;
                    case 40:
                        this.CsOdtSetup = "1/8";
                        break;
                    case 41:
                        this.CsOdtSetup = "1/9";
                        break;
                    case 42:
                        this.CsOdtSetup = "1/10";
                        break;
                    case 43:
                        this.CsOdtSetup = "1/11";
                        break;
                    case 44:
                        this.CsOdtSetup = "1/12";
                        break;
                    case 45:
                        this.CsOdtSetup = "1/13";
                        break;
                    case 46:
                        this.CsOdtSetup = "1/14";
                        break;
                    case 47:
                        this.CsOdtSetup = "1/15";
                        break;
                    case 48:
                        this.CsOdtSetup = "1/16";
                        break;
                    case 49:
                        this.CsOdtSetup = "1/17";
                        break;
                    case 50:
                        this.CsOdtSetup = "1/18";
                        break;
                    case 51:
                        this.CsOdtSetup = "1/19";
                        break;
                    case 52:
                        this.CsOdtSetup = "1/20";
                        break;
                    case 53:
                        this.CsOdtSetup = "1/21";
                        break;
                    case 54:
                        this.CsOdtSetup = "1/22";
                        break;
                    case 55:
                        this.CsOdtSetup = "1/23";
                        break;
                    case 56:
                        this.CsOdtSetup = "1/24";
                        break;
                    case 57:
                        this.CsOdtSetup = "1/25";
                        break;
                    case 58:
                        this.CsOdtSetup = "1/26";
                        break;
                    case 59:
                        this.CsOdtSetup = "1/27";
                        break;
                    case 60:
                        this.CsOdtSetup = "1/28";
                        break;
                    case 61:
                        this.CsOdtSetup = "1/29";
                        break;
                    case 62:
                        this.CsOdtSetup = "1/30";
                        break;
                    case 63:
                        this.CsOdtSetup = "1/31";
                        break;
                }

                uint ckeSetup = (physLong1 & 0xFF0000) >> 16;
                // ckeSetup / 32, ckeSetup % 32
                switch (ckeSetup)
                {
                    case 0:
                        this.CkeSetup = "0/0";
                        break;
                    case 1:
                        this.CkeSetup = "0/1";
                        break;
                    case 2:
                        this.CkeSetup = "0/2";
                        break;
                    case 3:
                        this.CkeSetup = "0/3";
                        break;
                    case 4:
                        this.CkeSetup = "0/4";
                        break;
                    case 5:
                        this.CkeSetup = "0/5";
                        break;
                    case 6:
                        this.CkeSetup = "0/6";
                        break;
                    case 7:
                        this.CkeSetup = "0/7";
                        break;
                    case 8:
                        this.CkeSetup = "0/8";
                        break;
                    case 9:
                        this.CkeSetup = "0/9";
                        break;
                    case 10:
                        this.CkeSetup = "0/10";
                        break;
                    case 11:
                        this.CkeSetup = "0/11";
                        break;
                    case 12:
                        this.CkeSetup = "0/12";
                        break;
                    case 13:
                        this.CkeSetup = "0/13";
                        break;
                    case 14:
                        this.CkeSetup = "0/14";
                        break;
                    case 15:
                        this.CkeSetup = "0/15";
                        break;
                    case 16:
                        this.CkeSetup = "0/16";
                        break;
                    case 17:
                        this.CkeSetup = "0/17";
                        break;
                    case 18:
                        this.CkeSetup = "0/18";
                        break;
                    case 19:
                        this.CkeSetup = "0/19";
                        break;
                    case 20:
                        this.CkeSetup = "0/20";
                        break;
                    case 21:
                        this.CkeSetup = "0/21";
                        break;
                    case 22:
                        this.CkeSetup = "0/22";
                        break;
                    case 23:
                        this.CkeSetup = "0/23";
                        break;
                    case 24:
                        this.CkeSetup = "0/24";
                        break;
                    case 25:
                        this.CkeSetup = "0/25";
                        break;
                    case 26:
                        this.CkeSetup = "0/26";
                        break;
                    case 27:
                        this.CkeSetup = "0/27";
                        break;
                    case 28:
                        this.CkeSetup = "0/28";
                        break;
                    case 29:
                        this.CkeSetup = "0/29";
                        break;
                    case 30:
                        this.CkeSetup = "0/30";
                        break;
                    case 31:
                        this.CkeSetup = "0/31";
                        break;
                    case 32:
                        this.CkeSetup = "1/0";
                        break;
                    case 33:
                        this.CkeSetup = "1/1";
                        break;
                    case 34:
                        this.CkeSetup = "1/2";
                        break;
                    case 35:
                        this.CkeSetup = "1/3";
                        break;
                    case 36:
                        this.CkeSetup = "1/4";
                        break;
                    case 37:
                        this.CkeSetup = "1/5";
                        break;
                    case 38:
                        this.CkeSetup = "1/6";
                        break;
                    case 39:
                        this.CkeSetup = "1/7";
                        break;
                    case 40:
                        this.CkeSetup = "1/8";
                        break;
                    case 41:
                        this.CkeSetup = "1/9";
                        break;
                    case 42:
                        this.CkeSetup = "1/10";
                        break;
                    case 43:
                        this.CkeSetup = "1/11";
                        break;
                    case 44:
                        this.CkeSetup = "1/12";
                        break;
                    case 45:
                        this.CkeSetup = "1/13";
                        break;
                    case 46:
                        this.CkeSetup = "1/14";
                        break;
                    case 47:
                        this.CkeSetup = "1/15";
                        break;
                    case 48:
                        this.CkeSetup = "1/16";
                        break;
                    case 49:
                        this.CkeSetup = "1/17";
                        break;
                    case 50:
                        this.CkeSetup = "1/18";
                        break;
                    case 51:
                        this.CkeSetup = "1/19";
                        break;
                    case 52:
                        this.CkeSetup = "1/20";
                        break;
                    case 53:
                        this.CkeSetup = "1/21";
                        break;
                    case 54:
                        this.CkeSetup = "1/22";
                        break;
                    case 55:
                        this.CkeSetup = "1/23";
                        break;
                    case 56:
                        this.CkeSetup = "1/24";
                        break;
                    case 57:
                        this.CkeSetup = "1/25";
                        break;
                    case 58:
                        this.CkeSetup = "1/26";
                        break;
                    case 59:
                        this.CkeSetup = "1/27";
                        break;
                    case 60:
                        this.CkeSetup = "1/28";
                        break;
                    case 61:
                        this.CkeSetup = "1/29";
                        break;
                    case 62:
                        this.CkeSetup = "1/30";
                        break;
                    case 63:
                        this.CkeSetup = "1/31";
                        break;
                }

                uint clkDrvStrength = (physLong1 & 0xFF000000) >> 24;
                if (clkDrvStrength <= 7)
                {
                    switch (clkDrvStrength)
                    {
                        case 0:
                            this.ClkDrvStrength = "120.0Ω";
                            break;
                        case 1:
                            this.ClkDrvStrength = "60.0Ω";
                            break;
                        case 2:
                            break;
                        case 3:
                            this.ClkDrvStrength = "40.0Ω";
                            break;
                        case 7:
                            this.ClkDrvStrength = "30.0Ω";
                            break;
                        default:
                            break;
                    }
                }
                else if (clkDrvStrength != 15)
                {
                    if (clkDrvStrength == 31)
                        this.ClkDrvStrength = "20.0Ω";
                }
                else
                    this.ClkDrvStrength = "24.0Ω";

                uint addrCmdDrvStrength = physLong2 & 0xFF;
                if (addrCmdDrvStrength <= 7)
                {
                    switch (addrCmdDrvStrength)
                    {
                        case 0:
                            this.AddrCmdDrvStrength = "120.0Ω";
                            break;
                        case 1:
                            this.AddrCmdDrvStrength = "60.0Ω";
                            break;
                        case 2:
                            break;
                        case 3:
                            this.AddrCmdDrvStrength = "40.0Ω";
                            break;
                        case 7:
                            this.AddrCmdDrvStrength = "30.0Ω";
                            break;
                        default:
                            break;
                    }
                }
                else if (addrCmdDrvStrength != 15)
                {
                    if (addrCmdDrvStrength == 31)
                        this.AddrCmdDrvStrength = "20.0Ω";
                }
                else
                    this.AddrCmdDrvStrength = "24.0Ω";

                uint csOdtDrvStrength = (physLong2 & 0xFF00) >> 8;
                if (csOdtDrvStrength <= 7)
                {
                    switch (csOdtDrvStrength)
                    {
                        case 0:
                            this.CsOdtDrvStrength = "120.0Ω";
                            break;
                        case 1:
                            this.CsOdtDrvStrength = "60.0Ω";
                            break;
                        case 2:
                            break;
                        case 3:
                            this.CsOdtDrvStrength = "40.0Ω";
                            break;
                        case 7:
                            this.CsOdtDrvStrength = "30.0Ω";
                            break;
                        default:
                            break;
                    }
                }
                else if (csOdtDrvStrength != 15)
                {
                    if (csOdtDrvStrength == 31)
                        this.CsOdtDrvStrength = "20.0Ω";
                }
                else
                    this.CsOdtDrvStrength = "24.0Ω";

                uint ckeDrvStrength = (physLong2 & 0xFF0000) >> 16;
                if (ckeDrvStrength <= 7)
                {
                    switch (ckeDrvStrength)
                    {
                        case 0:
                            this.CkeDrvStrength = "120.0Ω";
                            break;
                        case 1:
                            this.CkeDrvStrength = "60.0Ω";
                            break;
                        case 2:
                            break;
                        case 3:
                            this.CkeDrvStrength = "40.0Ω";
                            break;
                        case 7:
                            this.CkeDrvStrength = "30.0Ω";
                            break;
                        default:
                            break;
                    }
                }
                else if (ckeDrvStrength != 15)
                {
                    if (ckeDrvStrength == 31)
                        this.CkeDrvStrength = "20.0Ω";
                }
                else
                    this.CkeDrvStrength = "24.0Ω";

                // physLong2 byte 4?

                uint rttNom = physLong3 & 0xFF;
                switch (rttNom)
                {
                    case 0:
                        this.RttNom = "Disabled";
                        break;
                    case 1:
                        this.RttNom = "60.0Ω";
                        break;
                    case 2:
                        this.RttNom = "120.0Ω";
                        break;
                    case 3:
                        this.RttNom = "40.0Ω";
                        break;
                    case 4:
                        this.RttNom = "240.0Ω";
                        break;
                    case 5:
                        this.RttNom = "48.0Ω";
                        break;
                    case 6:
                        this.RttNom = "80.0Ω";
                        break;
                    case 7:
                        this.RttNom = "34.3Ω";
                        break;
                }

                uint rttWr = (physLong3 & 0xFF00) >> 8;
                switch (rttWr)
                {
                    case 0:
                        this.RttWr = "Disabled";
                        break;
                    case 1:
                        this.RttWr = "120.0Ω";
                        break;
                    case 2:
                        this.RttWr = "240.0Ω";
                        break;
                    case 3:
                        this.RttWr = "Hi-Z";
                        break;
                    case 4:
                        this.RttWr = "80.0Ω";
                        break;
                }

                uint rttPark = (physLong3 & 0xFF0000) >> 16;
                switch (rttPark)
                {
                    case 0:
                        this.RttPark = "Disabled";
                        break;
                    case 1:
                        this.RttPark = "60.0Ω";
                        break;
                    case 2:
                        this.RttPark = "120.0Ω";
                        break;
                    case 3:
                        this.RttPark = "40.0Ω";
                        break;
                    case 4:
                        this.RttPark = "240.0Ω";
                        break;
                    case 5:
                        this.RttPark = "48.0Ω";
                        break;
                    case 6:
                        this.RttPark = "80.0Ω";
                        break;
                    case 7:
                        this.RttPark = "34.3Ω";
                        break;
                }

                uint procODT = (physLong3 & 0xFF000000) >> 24;
                switch (procODT)
                {
                    case 8:
                        this.ProcODT = "120.0Ω";
                        break;
                    case 9:
                        this.ProcODT = "96.0Ω";
                        break;
                    case 10:
                        this.ProcODT = "80.0Ω";
                        break;
                    case 11:
                        this.ProcODT = "68.6Ω";
                        break;
                    default:
                        switch (procODT)
                        {
                            case 24:
                                this.ProcODT = "60.0Ω";
                                break;
                            case 25:
                                this.ProcODT = "53.3Ω";
                                break;
                            case 26:
                                this.ProcODT = "48.0Ω";
                                break;
                            case 27:
                                this.ProcODT = "43.6Ω";
                                break;
                            default:
                                switch (procODT)
                                {
                                    case 56:
                                        this.ProcODT = "40.0Ω";
                                        break;
                                    case 57:
                                        this.ProcODT = "36.9Ω";
                                        break;
                                    case 58:
                                        this.ProcODT = "34.3Ω";
                                        break;
                                    case 59:
                                        this.ProcODT = "32.0Ω";
                                        break;
                                    case 62:
                                        this.ProcODT = "30.0Ω";
                                        break;
                                    case 63:
                                        this.ProcODT = "28.2Ω";
                                        break;
                                }
                                break;
                        }
                        break;
                }
            }

            ols.WritePciConfigDword(0x0, 0xB8, 0x3B10528);
            ols.WritePciConfigDword(0x0, 0xBC, 0x02);
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

        public string ProcODT { get; private set; }
        public string RttNom { get; private set; }
        public string RttWr { get; private set; }
        public string RttPark { get; private set; }
        public string AddrCmdSetup { get; private set; }
        public string CsOdtSetup { get; private set; }
        public string CkeSetup { get; private set; }
        public string ClkDrvStrength { get; private set; }
        public string AddrCmdDrvStrength { get; private set; }
        public string CsOdtDrvStrength { get; private set; }
        public string CkeDrvStrength { get; private set; }
    }
}
