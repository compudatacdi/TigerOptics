﻿//==========================================================================================
//FixUpSvcConversion: Strings Extraction Completed at: 9/9/2012 9:24 PM
//==========================================================================================
/* Pre-processed by converter */
/*------------------------------------------------------------------------
    File        : ei/payment-JPMC.p
    Purpose     : Electronic Bank info in JPMorgan NACHA format. 
    Syntax      :
    Description : 
    Author(s)   : 
    Created     : 
    Notes       : Example AP EFT for JP Morgan
What needs to be done by the user:
- Create an Electronic interface file referencing this program
- Create an AP payment method referencing the electronic interface record
- Attach the AP payment method to a bank of a supplier
- Attach the AP payment method to the supplier
- customize Bank Account maintenance
- customize Supplier bank account (vendbank) in supplier maintenance

Here are the fields that have to be put into the UI through customization:

BankAcct:
//eb7:
//ImmediateOrigine        BankAcct.Character01
ImmediateOrigine        BankAcct.ImmediateOrigin_c

VendBank:
VendAccountType         VendBank.Character01    /* 22 = checking  * /
RoutingNumber           VendBank.Character02
RN checkdigit           VendBank.Character03

History:
03/23/12  KrisM  SCR 90748 - lines 6 and 8 were not getting output.
05/10/12  KrisM  SCR 90748 - additional changes not specified in original fix.

02/28/20 eb1 eric blackwelder @ cdi
	create from Payment_JPMC
	using this for the formatting:  https://www.chase.com/content/dam/chaseonline/en/demos/cbo/pdfs/cbo_achfile_helpguide.pdf
06/15/20 eb2 eric blackwelder @ cdi
	changes after first validation results
06/18/20 eb3 eric blackwelder @ cdi
	changes after 2nd validation results
06/23/20 eb4 eric blackwelder @ cdi
	changes after 3rd validation results
10/07/20 eb5 eric blackwelder @ cdi
	change where vendor bank info comes from
10/09/20 eb6 eric blackwelder @ cdi
	change hash totals
10/09/20 eb7 eric blackwelder @ cdi
	add record type 7 and other changes
05/25/21 eb8 eric blackwelder @ cdi
	tweaks to get record type 7 to work for JPM

  ----------------------------------------------------------------------*/
using System;
using Epicor.Utilities;
using System.IO;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using System.Collections.Generic;
using Epicor.Data;
using Erp;
using Erp.Tables;
using Ice;
using Ice.Lib;
using Erp.Internal.Lib;
//using Strings = Erp.Internal.EI.Resources.Strings;
using Epicor.Hosting;
using Erp.Services.Lib.Resources;
using BankBatching = Erp.Internal.Lib.BankBatching;
#if USE_EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif



namespace Erp.Internal.EI
{
    public partial class Payment_JPMC_PII : Ice.Libraries.ContextLibraryBase<ErpContext>,IProcessPayments, IBankBatching
    {
        /// ATTENTION!!!
        /// Storing Batch ID Generated by EI
        /// When the application creates AP payment batches, it generates a batch identifier - depending on the EI and
        /// payment method settings. The following functions used in the AP EI source files help you to set up accurate
        /// storing of the generated AP payment batch reference.
        /// However you can customize them as necessary.
        /// <summary>
        /// "GetPaymentBankBatchID" forms Batch ID.
        /// You must pass an ipRecord which holds the batch reference as a parameter value. To store the reference
        /// successfully, the function needs to return a non-empty string value from this object.
        /// <param name="ipRecord">Object holding Batch Id</param>/// 
        /// </summary> 
        public string GetPaymentBankBatchID(object ipRecord)
        {
            string result = string.Empty;
            return result;
        }
        /// <summary>
        /// "PayMethodGrouping" returns a value which specifies whether the EI program stores the batch reference:
        /// 1 - the EI program stores the batch reference
        /// 0 - the EI program does not store the batch reference.
        /// If you remove the code comment symbols in the commented part of the code, the batch reference will be stored
        /// according to the payment method settings.
        /// <param name="ipPMUID">Payment method UID</param>
        /// </summary>
        public bool PayMethodGrouping(int ipPMUID)
        {
            bool result = false;
            // if (LibBankBatching != null)
            //   result = LibBankBatching.PayMethodGrouping(ipPMUID); 
            return result;
        }
        /// <summary>
        /// "SavePaymentBankBatchID" saves the batch reference in an AR payment.
        /// <param name="ipHeadNum">Payment system number</param>
        /// <param name="ipBankBatchID">Non-empty Bank Batch Id</param>
        /// <param name="ipBatchDate">Bank Batch date</param>
        /// </summary>
        public void SavePaymentBankBatchID(int ipHeadNum, string ipBankBatchID, DateTime? ipBatchDate)
        {
            if (LibBankBatching != null && !String.IsNullOrEmpty(ipBankBatchID) && ipHeadNum != 0)
                LibBankBatching.SavePaymentBankBatchID(ipHeadNum, ipBankBatchID, BankBatching.SourceType.EICheckHed, Session.UserID, ipBatchDate, string.Empty);
//eb1: testing:
//this does not save a record to BankBatch table
//can't put in an int or string into the BankBatching.SourceType.EICheckHed field = get error
//LibBankBatching.SavePaymentBankBatchID(10001, "eb1", BankBatching.SourceType.EICheckHed, Session.UserID, ipBatchDate, string.Empty);

        }
        /// ATTENTION!!!
        int lineLen = 94;
        string OutputFile = string.Empty;

        #region Implicit buffers
        Erp.Tables.Company Company;
        Erp.Tables.BankAcct BankAcct;
        Erp.Tables.VendBank VendBank;
        Erp.Internal.EI.SFCommon.OutFileLine ttOutFileLine;
        #endregion

        private Lazy<Erp.Internal.EI.Payment_Def> _Payment_Def = new Lazy<Payment_Def>(() => new Erp.Internal.EI.Payment_Def());
        private Erp.Internal.EI.Payment_Def Payment_Def
        {
            get { return _Payment_Def.Value; }
        }

        private Lazy<Erp.Internal.EI.Payment_Common> _Payment_Common;
        private Erp.Internal.EI.Payment_Common Payment_Common
        {
            get { return _Payment_Common.Value; }
        }

        private Lazy<Erp.Internal.EI.SFCommon> _EISFCommon;
        private Erp.Internal.EI.SFCommon EISFCommon
        {
            get { return _EISFCommon.Value; }
        }

        private Lazy<FileName> libGetFileName;
        private FileName LibGetFileName { get { return libGetFileName.Value; } }

        private Lazy<BankBatching> libBankBatching;
        private BankBatching LibBankBatching
        {
            get { return this.libBankBatching.Value; }
        }

        protected override void Initialize()
        {
            this._Payment_Common = new Lazy<Payment_Common>(() => new Erp.Internal.EI.Payment_Common(this.Db));
            this._EISFCommon = new Lazy<SFCommon>(() => new Erp.Internal.EI.SFCommon(this.Db));
            libGetFileName = new Lazy<FileName>(() => new FileName(Db));
            libBankBatching = new Lazy<BankBatching>(() => new BankBatching(Db));
            base.Initialize();
        }


        public Payment_JPMC_PII(ErpContext ctx)
            : base(ctx)
        {
            this.Initialize();
        }

        SFCommon.OutFileLine ttOutFileLineRows = new SFCommon.OutFileLine();

        public void CreateFile(List<Payment_Def.TmpElec> ttTmpElecRows, int EFTHeadUID, string OutputFile)
        {
            Erp.Internal.EI.Payment_Def.TmpElec tmpElec = (from ttTmpElec_Row in ttTmpElecRows select ttTmpElec_Row).FirstOrDefault();
            bool storeBankBatchID = PayMethodGrouping(tmpElec != null ? tmpElec.SEPMUID : -1);
            this.buildInfo(ttTmpElecRows);
            OutputFile = LibGetFileName.Get(OutputFile, FileName.ServerFileType.Custom, false);
            //output stream outStream to value (OutputFile) unbuffered


            #region >>===== ABL Source ================================>>
            //
            //for each OutFileLine:
            //            
            //
            #endregion == ABL Source =================================<<

            using (var fileWriter = new writeFileLib(OutputFile, true))
            {
                foreach (var _OutFileLine in (from OutFileLine_Row in EISFCommon.ttOutFileLineRows
                                          select OutFileLine_Row))
                {
                    fileWriter.FileWriteLine(EISFCommon.SpecialCaps(_OutFileLine.Line_out.Substring(0, Math.Min(lineLen, _OutFileLine.Line_out.Length - 1))));
//eb1:
//storeBankBatchID = true;

                    if (storeBankBatchID)
                        SavePaymentBankBatchID(_OutFileLine.HeadNum, GetPaymentBankBatchID(string.Empty), tmpElec.ProcessDate);
                }
            }


        }
        ///<summary>
        ///  Parameters:  none
        ///</summary>
        /* stucture JPMorgan NACHA format:
            1 line of file header information
            1 line of batch header information   
                n Entry detail transaction records 
            1 line of batch control information
            1 line of File control information.
        */
        private void buildInfo(List<Payment_Def.TmpElec> ttTmpElecRows)
        {
            Payment_Def.TmpElec TmpElec = null;
            string ImmediateOrigine = string.Empty;
            DateTime? CurCheckDate = null;
            string CompanyBankAcct = string.Empty;
            string VendorBankNumber = string.Empty;
			
			//eb1:
            string SelfBankNumber = string.Empty;

            decimal TotalAmount = decimal.Zero;
            decimal TotalNumber = decimal.Zero;
            int Payment = 0;
            string STotalAmount = string.Empty;
            string STotalNumber = string.Empty;
            string SPayment = string.Empty;
            string Sblocks = string.Empty;
            string SAmount = string.Empty;
			//eb6:
            int Record6Count = 0;

            #region >>===== ABL Source ================================>>
            //
            //find first TmpElec no-lock no-error;
            //
            #endregion == ABL Source =================================<<

            TmpElec = (from ttTmpElec_Row in ttTmpElecRows select ttTmpElec_Row).FirstOrDefault();

            if (TmpElec == null)
            {
                throw new BLException(GlobalStrings.InterErrorTmpElecHasNoRecords);
            }
            else
            {
                CurCheckDate = TmpElec.ProcessDate;
            }



            #region >>===== ABL Source ================================>>
            //
            //FIND first Company WHERE Company.Company = CUR-COMP no-lock
            //
            #endregion == ABL Source =================================<<

            Company = this.FindFirstCompany(Session.CompanyID);
            if (Company == null)
            {
                throw new BLException(GlobalStrings.CompanyNotFound);
            }

			//eb7:
            //ImmediateOrigine = "1" + System.Text.RegularExpressions.Regex.Replace(Company.FEIN, "[^0-9]", ""); 

            #region >>===== ABL Source ================================>>
            //
            //FIND first BankAcct WHERE BankAcct.Company = Cur-comp and BankAcct.BankAcctID = TmpElec.FromBankAcctID no-lock
            //
            #endregion == ABL Source =================================<<


            BankAcct = this.FindFirstBankAcct(Session.CompanyID, TmpElec.FromBankAcctID);
            if (BankAcct == null)
            {
                throw new BLException(GlobalStrings.AValidFromBankAcctIsRequired);
            }
            CompanyBankAcct = ((10 > 0) ? "b".PadRight(10 + "b".Length, 'b') : null);
            if (BankAcct.CheckingAccount.Length <= 10)
            {
                ErpUtilities.Overlay(CompanyBankAcct, (11 - BankAcct.CheckingAccount.Length), BankAcct.CheckingAccount, 10);
            }
            else
            {
                throw new BLException(GlobalStrings.CheckingNumberIsTooLong);
            }
			
			//eb1:
			SelfBankNumber = String.Format("{0:00000000000000000000}", Convert.ToDouble(Payment_Common.BankAccount(BankAcct.CheckingAccount)));

			//eb7:
			ImmediateOrigine = BankAcct.ImmediateOrigin_c;


            if (EISFCommon.ttOutFileLineRows == null)
                EISFCommon.ttOutFileLineRows = new List<SFCommon.OutFileLine>();
            else EISFCommon.ttOutFileLineRows.Clear();

            /* 1 - file header information */
            ttOutFileLine = new SFCommon.OutFileLine();

            EISFCommon.ttOutFileLineRows.Add(ttOutFileLine);
            ttOutFileLine.Line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 0, "1", 1);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "01", 2);
			
			//eb1:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, CompanyBankAcct, 10);
			//spec example: 0021000021
			//BankAcct.CheckingAccount = 390198528
			//eb2:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, BankAcct.CheckingAccount, 10);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, " 021000021", 10);


			//eb2:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 13, ImmediateOrigine, 10);
            //eb7:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 13, "8184584039", 10);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 13, ImmediateOrigine, 10);

            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 23, Compatibility.Convert.ToString(CompanyTime.Today().Day, "99") + Compatibility.Convert.ToString(CompanyTime.Today().Month, "99") + Compatibility.Convert.ToString(CompanyTime.Today().Year, "9999").SubString(2, 2), 6);
            //eb7:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 23, DateTime.Now.ToString("yyMMdd"), 6);
			DateTime checkDate = (DateTime)TmpElec.CheckDate;
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 23, checkDate.ToString("yyMMdd"), 6);


            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 29, Compatibility.Convert.TimeToString(CompanyTime.Now(), "HH:MM").Replace(":", ""), 4);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 29, DateTime.Now.ToString("HHmm"), 4);

            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 33, Compatibility.Convert.ToString(CompanyTime.Today().Day, "99").SubString(1, 1), 1);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 33, "A", 1);


            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 34, "094", 3);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 37, "10", 2);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 39, "1", 1);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 40, "JPMORGAN CHASE", 23);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 63, Company.Name.ToUpperInvariant().SubString(0, 23), 23);/* long company name */


            /* 5 - batch header information */
            ttOutFileLine = new SFCommon.OutFileLine();

            EISFCommon.ttOutFileLineRows.Add(ttOutFileLine);
            ttOutFileLine.Line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 0, "5", 1);

			//eb1:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "200", 3);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "220", 3);

			//eb3:
			////eb1: chase wants this blank as per the spec
            ////ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 4, Company.Name.ToUpperInvariant().SubString(0, 16), 16);  /* short company name */
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 4, "", 16);
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 4, Company.Name.ToUpperInvariant().SubString(0, 16), 16);  /* short company name */

			//eb1: was missing, not sure why:
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 20, SelfBankNumber, 20);


			// COMPANY IDENTIFICATION (Assigned by JPMC)
            //eb2:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 40, ImmediateOrigine, 10);
            //eb7:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 40, "9198528000", 10);
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 40, ImmediateOrigine, 10);

			// STANDARD ENTRY CLASS CODE
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 50, "PPD", 3);
            //eb8:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 50, "CCD", 3);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 50, "CTX", 3);

            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 53, "PAYROLL", 10);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 53, "ACH PMT", 10);

            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 63, GlobalStrings.JANFEBMARAPRMAYJUNJULAUGSEPOCTNOVDEC.Entry(CompanyTime.Today().Month - 1, ',') + " " + Compatibility.Convert.ToString(CompanyTime.Today().Year, "9999").SubString(2, 2), 6);
			//eb7:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 63, DateTime.Now.ToString("MMM dd"), 6);
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 63, checkDate.ToString("MMM dd"), 6);

			
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 69, Compatibility.Convert.ToString(((DateTime)CurCheckDate).Day, "99") + Compatibility.Convert.ToString(((DateTime)CurCheckDate).Month, "99") + Compatibility.Convert.ToString(((DateTime)CurCheckDate).Year, "9999").SubString(2, 2), 6);
			//eb7:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 69, DateTime.Now.ToString("yyMMdd"), 6);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 69, checkDate.ToString("yyMMdd"), 6);

            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 75, "", 3);


            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 78, "1", 1);
            
			// bank routing number first 8 digits
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, ImmediateOrigine.SubString(1, 8), 8);
			//eb3:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, BankAcct.BankRoutingNum.SubString(0, 8), 8);
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, "02100002", 8);

			// batch number
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 87, "0000001", 7);

            /* transactions */


            #region >>===== ABL Source ================================>>
            //
            //for each TmpElec:
            //              
            //      
            //    
            //
            #endregion == ABL Source =================================<<

            foreach (var _TmpElec in ttTmpElecRows)
            {
                TmpElec = _TmpElec;

                #region >>===== ABL Source ================================>>
                //
                //FIND first VendBank WHERE VendBank.Company = cur-comp and
                //                                  VendBank.VendorNum = TmpElec.VendorNum and
                //                                  VendBank.BankID = TmpElec.VendorBankID  no-lock
                //
                #endregion == ABL Source =================================<<

                VendBank = this.FindFirstVendBank(Session.CompanyID, TmpElec.VendorNum, TmpElec.VendorBankID);
                if (VendBank == null)
                {
                    throw new BLException(GlobalStrings.AValidVendorBankIsRequired);
                }
                VendorBankNumber = Payment_Common.FillZero(Payment_Common.GetOnlyNumbers(TmpElec.VendorBankAcctNumber), 17);
                TotalAmount = TotalAmount + (TmpElec.DocCheckAmt);
                
				//eb3:
				//TotalNumber = TotalNumber + Compatibility.Convert.ToInt32(VendBank.DFIIdentification);
				//eb4: the last digit in the BankRoutingNum is a check digit so need to strip it off
				//TotalNumber = TotalNumber + Compatibility.Convert.ToInt32(BankAcct.BankRoutingNum);
				TotalNumber = TotalNumber + Compatibility.Convert.ToInt32(BankAcct.BankRoutingNum.Substring(0, BankAcct.BankRoutingNum.Length - 1) );
				
                Payment = Payment + 1;
                if (String.IsNullOrEmpty(TmpElec.VendorBankAcctNumber))
                {
                    throw new BLException(GlobalStrings.AValidSupplBankAccountNumberIsRequi);
                }
                SAmount = this.cnvAmount(Compatibility.Convert.ToString((TmpElec.DocCheckAmt * 100), "9999999999"), 10);
  
  
				/* 6 - Entry Detail transaction information */
                ttOutFileLine = new SFCommon.OutFileLine();

				//eb6:
				Record6Count += 1;
				
                EISFCommon.ttOutFileLineRows.Add(ttOutFileLine);
                ttOutFileLine.HeadNum = TmpElec.HeadNum;
                ttOutFileLine.Line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
                ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 0, "6", 1);
                ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "22", 2);    /*vendbank.Character03 or hardcode:"22":U   checking */

				//eb2:  changes needed here: sh be...  position 4-12 : Receiving DFI Routing Number
				//		that was definition in one doc, the proper doc says:
				//			4-11	RECEIVING DFI ID	Must be a valid Routing Number
				//			12-12	CHECK DIGIT			Routing Number Check Digit
				//		SOOO... the last digit in routing number is a check digit
				//		SOOO... when using routing number later, strip off the last digit
                //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, ((VendBank.DFIIdentification.Length > 7) ? VendBank.DFIIdentification.Substring(0, 8) : string.Empty), 8);  /* ROUTING NUMBER */
                //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 11, ((VendBank.DFIIdentification.Length > 8) ? VendBank.DFIIdentification.Substring(8, 1) : string.Empty), 1);  /* ROUTING NUMBER CHECK DIGIT */
				//eb5:
				//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, BankAcct.BankRoutingNum, 9);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, VendBank.SwiftNum, 9);
				
				//eb2:  changes needed here: sh be...  position 13-29 : Receving DFI account number
                //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 12, VendorBankNumber, 17);
				//eb5:
				//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 12, BankAcct.CheckingAccount, 17);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 12, VendBank.BankAcctNumber, 17);

				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 29, SAmount, 10);
                ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 39, TmpElec.VendorBankID, 15);  /* or maybe tmpElect.VendorAccountRef ?*/

//zzz                
				//eb8:
                //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 54, TmpElec.VendorBankNameOnAccount.Trim().SubString(0, 22), 22);
                //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 78, "0", 1);				
				////eb3:
                ////ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, ImmediateOrigine.SubString(1, 8) + Compatibility.Convert.ToString(Payment, "9999999"), 15);
				//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, "02100002", 8);
				//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 87, "0000001", 7);
				int record7TotalCount = 0;
				int record7Yes = 0;
				foreach (var APTran_iterator in (this.SelectAPTran(Session.CompanyID, TmpElec.HeadNum)))
				{
					if (APTran_iterator.InvoiceNum != "")
					{
						record7TotalCount += 1;
						record7Yes = 1;
					}
				}
                ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 54, record7TotalCount.ToString("D4"), 4);
                ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 58, TmpElec.VendorBankNameOnAccount.Trim().SubString(0, 16), 16);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 74, "  ", 2);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 76, "  ", 2);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 78, record7Yes.ToString(), 1);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, "02100002", 8);
				ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 87, "0000001", 7);



				//eb7:
				//invoices:
				//this is a list of invoice numbers
				/* 7 - Addenda Record */
				//eb8: moved this up:
				/*
				int record7TotalCount = 0;
				foreach (var APTran_iterator in (this.SelectAPTran(Session.CompanyID, TmpElec.HeadNum)))
				{
					if (APTran_iterator.InvoiceNum != "")
					{
						record7TotalCount += 1;
					}
				}
				*/

				int record7Counter = 0;
				foreach (var APTran_iterator in (this.SelectAPTran(Session.CompanyID, TmpElec.HeadNum)))
				{
					if (APTran_iterator.InvoiceNum != "")
					{
						record7Counter += 1;

						ttOutFileLine = new SFCommon.OutFileLine();
						ttOutFileLine.Line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);				
						EISFCommon.ttOutFileLineRows.Add(ttOutFileLine);

						ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 0, "7", 1);
						//eb8:
						//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "200", 3);
						ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "05", 2);

						ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 3, "Invoice " + APTran_iterator.InvoiceNum.ToString(), 80);

						//KEEP for testing:
						//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 40, "HeadNum " + TmpElec.HeadNum, 40);						
						
						ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 83, record7TotalCount.ToString("D4"), 4);
						ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 87, record7Counter.ToString("D7"), 7);
					}

				}
				
            }/* for each TmpElec: */
            /* convert amounts and numbers to string and replace speces with zeros */

            STotalAmount = this.cnvAmount(Compatibility.Convert.ToString((TotalAmount * 100), "9999999999"), 12);
			//eb6:
            //STotalNumber = this.cnvAmount(Compatibility.Convert.ToString(TotalNumber), 10);
			STotalNumber = this.cnvAmount(Compatibility.Convert.ToString(Record6Count * 02100002), 10);


            SPayment = this.cnvAmount(Compatibility.Convert.ToString(Payment), 6);
            Sblocks = this.cnvAmount(Compatibility.Convert.ToString(Payment + 4), 6);


			

            /* 8 - Batch control record information */
            ttOutFileLine = new SFCommon.OutFileLine();

            EISFCommon.ttOutFileLineRows.Add(ttOutFileLine);
            ttOutFileLine.Line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 0, "8", 1);
			
            //eb1:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "200", 3);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "220", 3);
            
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 4, SPayment, 6);

			//The sum of positions 4-11 of all Entry Detail (6) Records in the batch.
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 10, STotalNumber, 10);
			
			//Must equal the total debit dollar amount in the batch.
			//eb4:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 20, STotalAmount, 12);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 20, "000000000000", 12);
			
			//Must equal the total credit dollar amount in the batch.
			//eb4:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 32, "0", 12);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 32, STotalAmount, 12);

            //eb2:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 44, ImmediateOrigine, 10);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 44, "9198528000", 10);

            //eb2:
			//ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, ImmediateOrigine.SubString(1, 8), 8);
			ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 79, "02100002", 8);


            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 87, "0000001", 7);


            /* 9 - file control record information */
            ttOutFileLine = new SFCommon.OutFileLine();

            EISFCommon.ttOutFileLineRows.Add(ttOutFileLine);
            ttOutFileLine.Line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 0, "9", 1);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 1, "000001", 6);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 7, Sblocks, 6);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 13, "00" + SPayment, 8);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 21, STotalNumber, 10);
			
			//eb4:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 31, STotalAmount, 12);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 31, "000000000000", 12);
			
			//eb4:
            //ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 43, "0", 12);
            ttOutFileLine.Line_out = ErpUtilities.Overlay(ttOutFileLine.Line_out, 43, STotalAmount, 12);

        }
        private string cnvAmount(string Amount, int Alength)
        {
            int old_pos = 0;
            int new_pos = 0;
            string result = string.Empty;
            result = ((Alength > 0) ? "0".PadRight(Alength + "0".Length, '0') : null);
            new_pos = Alength;
            for (old_pos = 0; old_pos <= Amount.Length; old_pos++)
            {
                result = ErpUtilities.Overlay(result, new_pos, Amount.SubString(Amount.Length + 1 - old_pos - 1, 1), 1);
                new_pos = new_pos - 1;
                if (new_pos == 0)
                {
                    break;
                }
            } 
            return result;
        }
    }
}
