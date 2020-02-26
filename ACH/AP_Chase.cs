﻿//==========================================================================================
//FixUpSvcConversion: Strings Extraction Completed at: 9/9/2012 9:24 PM
//==========================================================================================
/* Pre-processed by converter */
/*------------------------------------------------------------------------
    File        : ei/payment-BTL91.p
    Purpose     : Electronic Bank info in BTL91 format. 
    Syntax      :
    Description : This file replaces ap/app20-BTL91.w
    Author(s)   : Tatyana Kozmina
    Created     : 06-09-2008
    Notes       :
Revision History:
-----------------
06/09/08 TatyanaK SCR 40680 - Soft format for electronic payments was implemented.
06/23/09 JHMartinez SCR 58256 - References to Vendor were replaced with Supplier.
07/07/09 galinad SCR 64216 - buildInfo procedure - the logic to equate IsoCurrencySymbol = "EUR" is commented out to avoid error message.
05/14/10 jajohnson scr 67341 - CheckHed may be a different currency than APChkGrp.

02/26/20 eblackwelder cdi create AP_Chase from Payment_BTL91
	using this for the formatting:  https://www.chase.com/content/dam/chaseonline/en/demos/cbo/pdfs/cbo_achfile_helpguide.pdf

  ----------------------------------------------------------------------*/
using System;
using Epicor.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Transactions;
using Epicor.Data;
using Erp;
using Erp.Tables;
using Ice;
//using Strings = Erp.Internal.EI.Resources.Strings;
using Epicor.Hosting;
using Erp.Internal.EI;
using Ice.Core;
using Erp.Internal.Lib;
using Erp.Services.Lib.Resources;
using BankBatching = Erp.Internal.Lib.BankBatching;
using Ice.Lib;
#if USE_EF_CORE
using Microsoft.EntityFrameworkCore;
#else
using System.Data.Entity;
#endif

namespace Erp.Internal.EI
{
    public partial class AP_Chase : Ice.Libraries.ContextLibraryBase<ErpContext>, IProcessPayments, IBankBatching
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
        /// "SavePaymentBankBatchID" saves the batch reference in an AP payment.
        /// <param name="ipHeadNum">Payment system number</param>
        /// <param name="ipBankBatchID">Non-empty Bank Batch Id</param>
        /// <param name="ipBatchDate">Bank Batch date</param>
        /// </summary>
        public void SavePaymentBankBatchID(int ipHeadNum, string ipBankBatchID, DateTime? ipBatchDate)
        {
            if (LibBankBatching != null && !String.IsNullOrEmpty(ipBankBatchID) && ipHeadNum != 0)
                LibBankBatching.SavePaymentBankBatchID(ipHeadNum, ipBankBatchID, BankBatching.SourceType.EICheckHed, Session.UserID, ipBatchDate, string.Empty);
        }
        /// ATTENTION!!!
        public class TmpCurrTotal
        {
            public decimal TotalAmount { get; set; }
            public int TotalPayments { get; set; }
            private string _ISOCurrencyCode = string.Empty;
            public string ISOCurrencyCode
            {
                get { return this._ISOCurrencyCode; }
                set { this._ISOCurrencyCode = value; }
            }
        }

        public TmpCurrTotal ttTmpCurrTotal;
        public List<TmpCurrTotal> ttTmpCurrTotalRows = new List<TmpCurrTotal>();
        int lineLen = 512;
        #region Implicit buffers
        Erp.Tables.Currency Currency;
        Erp.Tables.Company Company;
        Erp.Tables.Country Country;
        Erp.Tables.BankAcct BankAcct;
        Erp.Tables.APTran APTran;

        #endregion
        private Lazy<Erp.Internal.EI.Payment_Common> _Payment_Common;
        private Erp.Internal.EI.Payment_Common Payment_Common { get { return _Payment_Common.Value; } }
        private Lazy<Erp.Internal.EI.SFCommon> _SFCommon;
        private Erp.Internal.EI.SFCommon SFCommon { get { return _SFCommon.Value; } }
        private Lazy<Ice.Lib.FileName> _FileName;
        private Ice.Lib.FileName FileName { get { return _FileName.Value; } }
        private Lazy<BankBatching> libBankBatching;
        private BankBatching LibBankBatching
        {
            get { return this.libBankBatching.Value; }
        }

        protected override void Initialize()
        {
            this._Payment_Common = new Lazy<Erp.Internal.EI.Payment_Common>(() => new Erp.Internal.EI.Payment_Common(this.Db));
            this._SFCommon = new Lazy<Erp.Internal.EI.SFCommon>(() => new Erp.Internal.EI.SFCommon(this.Db));
            this._FileName = new Lazy<Ice.Lib.FileName>(() => new Ice.Lib.FileName(this.Db));
            this._Payment_Common = new Lazy<Erp.Internal.EI.Payment_Common>(() => new Erp.Internal.EI.Payment_Common(Db)); 
            this._SFCommon = new Lazy<Erp.Internal.EI.SFCommon>(() => new Erp.Internal.EI.SFCommon(Db)); 
            this._FileName = new Lazy<Ice.Lib.FileName>(() => new Ice.Lib.FileName(Db));
            libBankBatching = new Lazy<BankBatching>(() => new BankBatching(Db));
            base.Initialize();
        }
        public AP_Chase(ErpContext ctx)
            : base(ctx)
        {
            this.Initialize();
        }

        public void CreateFile(List<Erp.Internal.EI.Payment_Def.TmpElec> TmpElecRows, int EFTHeadUID, string OutputFile)
        {
            Erp.Internal.EI.Payment_Def.TmpElec tmpElec = (from ttTmpElec_Row in TmpElecRows select ttTmpElec_Row).FirstOrDefault();
            bool storeBankBatchID = PayMethodGrouping(tmpElec != null ? tmpElec.SEPMUID : -1);
            this.buildInfo(TmpElecRows);
            OutputFile = FileName.Get(OutputFile, Ice.Lib.FileName.ServerFileType.Custom, false);
            //output stream outStream to value (OutputFile) unbuffered

            #region >>===== ABL Source ================================>>
            //
            //for each OutFileLine:
            //            
            //
            #endregion == ABL Source =================================<<

            using (var fileWriter = new Ice.Lib.writeFileLib(OutputFile, true))
            {
                foreach (var _OutFileLine in (from OutFileLine_Row in SFCommon.ttOutFileLineRows
                                              select OutFileLine_Row))
                {
                    fileWriter.FileWriteLine(SFCommon.SpecialCaps(_OutFileLine.Line_out.Substring(0, Math.Min(lineLen, _OutFileLine.Line_out.Length - 1))));
                    if (storeBankBatchID)
                        SavePaymentBankBatchID(_OutFileLine.HeadNum, GetPaymentBankBatchID(string.Empty), tmpElec.ProcessDate);
                }
            }

        }
        ///<summary>
        ///  Parameters:  none
        ///</summary>
        /* structure btl91 format:
            1 line of file information
            1 batches consisting of
              x payments consisting of 
                  4 payment transaction records
              y currency total records   
            1 line of file closing information.
         */
        private void buildInfo(List<Erp.Internal.EI.Payment_Def.TmpElec> TmpElecRows)
        {
            string CurGroupCurrencyCode = string.Empty;
            DateTime? CurCheckDate = null;
            string BankCurrencySymbol = string.Empty;
            string VendorBankISOCountryCode = string.Empty;
            string VendorISOCountryCode = string.Empty;
            string SAmount = string.Empty;
            string TAmount = string.Empty;
            int TotalRecords = 0;
            string PaymentNumber = string.Empty;
            int TotalOrders = 0;
            string Selfbanknumber = string.Empty;
            string Description_1 = string.Empty;
            int TotalInvoice = 0;
            string TransferCost = string.Empty;
            string VendorBankSWIFTAddress = string.Empty;
            string IsoCurrencySymbol = string.Empty;
            string v_CurrCode = string.Empty;
            int lineLevel = 0;
            SFCommon.OutFileLine OutFileLineRow;

            #region >>===== ABL Source ================================>>
            //
            //FIND first Currency WHERE Currency.Company = cur-comp and 
            //                              Currency.BaseCurr = true no-lock
            //
            #endregion == ABL Source =================================<<

            Currency = this.FindFirstCurrency(Session.CompanyID, true);
            if (Currency != null)
            {
                v_CurrCode = Currency.CurrencyCode;
            }
            else
            {
                v_CurrCode = "";
            }


            #region >>===== ABL Source ================================>>
            //
            //find first TmpElec no-lock no-error;
            //
            #endregion == ABL Source =================================<<

            var TmpElec = (from row in TmpElecRows select row).FirstOrDefault();
            if (TmpElec == null)
            {
                throw new BLException(GlobalStrings.InterErrorTmpElecHasNoRecords);
            }
            else
            {
                CurGroupCurrencyCode = TmpElec.GroupCurrCode;
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


            #region >>===== ABL Source ================================>>
            //
            //FIND first Country WHERE Country.Company = CUR-COMP and Country.CountryNum = Company.CountryNum no-lock
            //
            #endregion == ABL Source =================================<<

            Country = this.FindFirstCountry(Session.CompanyID, Company.CountryNum);
            if (Country == null)
            {
                throw new BLException(GlobalStrings.AValidCountryOfYourCompanyIsRequi);
            }


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
            Selfbanknumber = Payment_Common.BankAccount(BankAcct.CheckingAccount);
            BankCurrencySymbol = GetCurrencyID(BankAcct.CurrencyCode);
            IsoCurrencySymbol = GetCurrencyID(CurGroupCurrencyCode);/* CurGroupCurrencyCode will be blank if more than one currency in the group 67341*/

            /* The line below is commented out per SCR # 64216 */
            /*if CurGroupCurrencyCode = v-CurrCode then IsoCurrencySymbol = "EUR":U.*/

            if (String.IsNullOrEmpty(Selfbanknumber))
            {
                throw new BLException(GlobalStrings.AValidYourBankAccountNumberIsRequi);
            }
            /* if the currency of the bankaccount is not Dutch Guilder or Euro then
               the currency of the bankaccount must be equal to the currency of the payment */
            if ((!(BankCurrencySymbol.Compare("NLG") == 0 || BankCurrencySymbol.Compare("EUR") == 0 || BankCurrencySymbol.Compare("?") == 0)) && (IsoCurrencySymbol.Compare(BankCurrencySymbol) != 0))
            {
                throw new BLException(GlobalStrings.OnlyPaymeInTheCurreOfThisBankAccountAreAllowed);
            }
            SFCommon.ttOutFileLineRows = new List<SFCommon.OutFileLine>();
            SFCommon.ttOutFileLineRows.Clear();
            /* 11: file information */
            lineLevel = 11;
            TotalRecords = 0;
            /* This code adds information from the specified payment method into a document.
               The document will have the structure like this:

                1 line of payment method information
                1 line of file information
                1 batches consisting of
                  x payments consisting of
                      4 payment transaction records
                  y currency total records
                1 line of file closing information.

            SCR 40680  - uncomment the string below if you want insert payment method information  */
            /*
            create OutFileLine.
            {ei/SoftFormat.i &TableName        = PayMethodProp
                             &EFTHeadUID       = EFTHeadUID
                             &LineOut          = OutFileLine.Line-out
                             &LineLen          = lineLen
                             &LineLevel        = lineLevel
                             &LineLevelFormat  = lineLevelFormat
                             &TotalRecords     = TotalRecords}
            */
            OutFileLineRow = new SFCommon.OutFileLine();
            SFCommon.ttOutFileLineRows.Add(OutFileLineRow);

			// FILE HEADER RECORD (1)

            string line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
            // field 1
            line_out = ErpUtilities.Overlay(line_out, 0, "1", 1);
            // field 2
            line_out = ErpUtilities.Overlay(line_out, 1, "01", 2);
            // field 3
            line_out = ErpUtilities.Overlay(line_out, 3, "0021000021", 10);
            // field 4
            line_out = ErpUtilities.Overlay(line_out, 13, "0000000000", 10);
            // field 5
            line_out = ErpUtilities.Overlay(line_out, 23, DateTime.Now.ToString("yyMMdd"), 6);
            // field 6
            line_out = ErpUtilities.Overlay(line_out, 29, DateTime.Now.ToString("HHmm"), 4);

//zzz
            // field 7
//            line_out = ErpUtilities.Overlay(line_out, 20, Company.Name, 35);
            line_out = ErpUtilities.Overlay(line_out, 33, "A", 1);

            // field 8
//            line_out = ErpUtilities.Overlay(line_out, 55, Company.Address1, 35);
            line_out = ErpUtilities.Overlay(line_out, 34, "094", 3);

            // field 9
//            line_out = ErpUtilities.Overlay(line_out, 90, ((!String.IsNullOrEmpty(Company.Zip)) ? (Company.Zip + " " + Company.City) : Company.City), 35);
            line_out = ErpUtilities.Overlay(line_out, 37, "10", 2);

            // field 10
            //line_out = ErpUtilities.Overlay(line_out, 125, Country.Description, 35);
            line_out = ErpUtilities.Overlay(line_out, 39, "1", 1);

            // field 11
//            line_out = ErpUtilities.Overlay(line_out, 160, "0000", 4);
            line_out = ErpUtilities.Overlay(line_out, 40, "JPMORGAN CHASE", 23);

            // field 12
//            line_out = ErpUtilities.Overlay(line_out, 164, Compatibility.Convert.ToString(((DateTime)CurCheckDate).Year, "9999") + Compatibility.Convert.ToString(((DateTime)CurCheckDate).Month, "99") + Compatibility.Convert.ToString(((DateTime)CurCheckDate).Day, "99"), 8);
            line_out = ErpUtilities.Overlay(line_out, 63, Company.Name, 23);

            // field 13
            line_out = ErpUtilities.Overlay(line_out, 86, "", 8);

//*/

            TotalRecords = TotalRecords + 1;
            lineLevel = lineLevel + 1;
            PaymentNumber = "";
            TotalOrders = 0;
            OutFileLineRow.Line_out = line_out;

            /* This code adds information from the specified payment method into a document.
               The document will have the structure like this:

                1 line of file information
                1 line of payment method information
                1 batches consisting of
                  x payments consisting of
                      4 payment transaction records
                  y currency total records
                1 line of file closing information.

            SCR 40680  - uncomment the string below if you want insert payment method information  */
            /*
            create OutFileLine.
            {ei/SoftFormat.i &TableName        = PayMethodProp
                             &EFTHeadUID       = EFTHeadUID
                             &LineOut          = OutFileLine.Line-out
                             &LineLen          = lineLen
                             &LineLevel        = lineLevel
                             &LineLevelFormat  = lineLevelFormat
                             &TotalRecords     = TotalRecords}
            */
            
            #region >>===== ABL Source ================================>>
            //
            //for each TmpElec no-lock:
            //                         
            //        
            //
            #endregion == ABL Source =================================<<

            foreach (var _TmpElec in TmpElecRows)
            {
                TmpElec = _TmpElec;
                lineLevel = 21;
                Description_1 = "";
                TotalInvoice = 0;
                TotalOrders = TotalOrders + 1;
                TransferCost = "3";
                PaymentNumber = Compatibility.Convert.ToString(TotalOrders, "9999");
                VendorBankSWIFTAddress = TmpElec.VendorBankSwiftNum;
                IsoCurrencySymbol = GetCurrencyID(TmpElec.CurrencyCode);
                if (String.IsNullOrEmpty(TmpElec.VendorBankAcctNumber))
                {
                    throw new BLException(GlobalStrings.AValidSupplBankAccountNumberIsRequi);
                }


                #region >>===== ABL Source ================================>>
                //
                //FIND first Country WHERE Country.Company = CUR-COMP and Country.CountryNum = TmpElec.VendorCountryNum no-lock
                //
                #endregion == ABL Source =================================<<

                Country = this.FindFirstCountry2(Session.CompanyID, TmpElec.VendorCountryNum);
                if (Country == null)
                {
                    throw new BLException(GlobalStrings.AValidCountryOfTheSupplIsRequi);
                }
                if (String.IsNullOrEmpty(Country.ISOCode))
                {
                    throw new BLException(GlobalStrings.TheISOCountryCodeOfTheSupplIsRequi(TmpElec.VendorName));
                }
                VendorISOCountryCode = Country.ISOCode;


                #region >>===== ABL Source ================================>>
                //
                //FIND first Country WHERE Country.Company = CUR-COMP and Country.CountryNum = TmpElec.VendorBankCountryNum no-lock
                //
                #endregion == ABL Source =================================<<

                Country = this.FindFirstCountry3(Session.CompanyID, TmpElec.VendorBankCountryNum);
                if (Country == null)
                {
                    throw new BLException(GlobalStrings.AValidCountryOfTheBankOfTheSupplIsRequi);
                }
                if (String.IsNullOrEmpty(Country.ISOCode))
                {
                    throw new BLException(GlobalStrings.AValidCountryISOCodeOfTheBankOfTheSupplIsRequi);
                }
                VendorBankISOCountryCode = Country.ISOCode;
                /* fill the Description with invoice numbers,
                  unless there are too many invoices */


                #region >>===== ABL Source ================================>>
                //
                //for each APTran where APTran.Company = Cur-Comp and APTran.HeadNum = TmpElec.HeadNum no-lock
                //                             by APTran.Company
                //                               by APTran.HeadNum
                //                                 by APTranNo
                //                                   by InvoiceNum:
                //                                   
                //                 
                //            
                //
                #endregion == ABL Source =================================<<

                //invoices:
                foreach (var APTran_iterator in (this.SelectAPTran(Session.CompanyID, TmpElec.HeadNum)))
                {
                    APTran = APTran_iterator;
                    if (!String.IsNullOrEmpty(APTran.InvoiceNum) && TotalInvoice < 35)
                    {
                        if (TotalInvoice == 0)
                        {
                            TotalInvoice = TotalInvoice + 1;
                            Description_1 = GlobalStrings.Ref(APTran.InvoiceNum);
                        }
                        else if (Description_1.Length + APTran.InvoiceNum.Length <= 35)
                        {
                            TotalInvoice = TotalInvoice + 1;
                            Description_1 = Description_1 + APTran.InvoiceNum + " ";
                        }
                        else
                        {
                            TotalInvoice = 99;
                            Description_1 = "";
                            break;
                        }
                    }
                }
                SAmount = this.cnvAmount(IsoCurrencySymbol, Compatibility.Convert.ToString((TmpElec.DocCheckAmt * 100), "999999999999999"), 14);
                /* 21: batch payment record part 1 */
                OutFileLineRow = new SFCommon.OutFileLine();
                SFCommon.ttOutFileLineRows.Add(OutFileLineRow);
                line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
                /*  1 */
                line_out = ErpUtilities.Overlay(line_out, 0, Compatibility.Convert.ToString(lineLevel), 2);
                /*  2 */
                line_out = ErpUtilities.Overlay(line_out, 2, PaymentNumber, 4);
                /*  3 */
                line_out = ErpUtilities.Overlay(line_out, 6, BankCurrencySymbol, 3);
                /*  4 */
                line_out = ErpUtilities.Overlay(line_out, 9, Selfbanknumber, 10);
                /*  5 */
                line_out = ErpUtilities.Overlay(line_out, 19, IsoCurrencySymbol, 3);
                /*  6 */
                line_out = ErpUtilities.Overlay(line_out, 22, SAmount, 15);
                /*  7 */
                line_out = ErpUtilities.Overlay(line_out, 37, Compatibility.Convert.ToString(((DateTime)CurCheckDate).Year, "9999") + Compatibility.Convert.ToString(((DateTime)CurCheckDate).Month, "99") + Compatibility.Convert.ToString(((DateTime)CurCheckDate).Day, "99"), 8);
                /*  8 */
                line_out = ErpUtilities.Overlay(line_out, 45, "1", 1);         /* Standard for euro transfer */
                /*  9 */
                line_out = ErpUtilities.Overlay(line_out, 46, TransferCost, 1);
                /* 10 */
                line_out = ErpUtilities.Overlay(line_out, 47, "0", 1);         /* banktransfer */
                /* 11 */
                line_out = ErpUtilities.Overlay(line_out, 48, "0", 1);         /* Swift normal speed */
                /* 12 */
                line_out = ErpUtilities.Overlay(line_out, 49, " ", 1);         /* not allowed for euro transfer */
                /* 13 */
                line_out = ErpUtilities.Overlay(line_out, 50, " ", 1);         /* not allowed for euro transfer */
                /* 14 */
                line_out = ErpUtilities.Overlay(line_out, 51, "  ", 2);        /* not allowed for euro transfer */
                /* 15 */
                line_out = ErpUtilities.Overlay(line_out, 53, "  ", 2);        /* not allowed for euro transfer */
                /* 16 */
                line_out = ErpUtilities.Overlay(line_out, 55, "  ", 2);        /* not allowed for euro transfer */
                /* 17 */
                line_out = ErpUtilities.Overlay(line_out, 57, "  ", 2);        /* not allowed for euro transfer */
                /* 18 */
                line_out = ErpUtilities.Overlay(line_out, 59, "1", 1);
                /* 19 */
                /* not required */
                /* 20 */
                /* not required */
                /* 21 */
                /* not required */
                /* 22 */
                /* not required */
                /* 23 */
                /* not required */
                /* 24 */
                /* not required */
                TotalRecords = TotalRecords + 1;
                lineLevel = lineLevel + 1;
                OutFileLineRow.Line_out = line_out;
                OutFileLineRow.HeadNum = TmpElec.HeadNum;
                /* 22: batch payment record part 2 */
                OutFileLineRow = new SFCommon.OutFileLine();
                SFCommon.ttOutFileLineRows.Add(OutFileLineRow);
                line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
                /*  1 */
                line_out = ErpUtilities.Overlay(line_out, 0, Compatibility.Convert.ToString(lineLevel), 2);
                /*  2 */
                line_out = ErpUtilities.Overlay(line_out, 2, PaymentNumber, 4);
                /*  3 */
                line_out = ErpUtilities.Overlay(line_out, 6, TmpElec.VendorBankAcctNumber, 34);
                /*  4 */
                line_out = ErpUtilities.Overlay(line_out, 40, ((TmpElec.VendorBankNameOnAccount.Trim().Length > 0) ? TmpElec.VendorBankNameOnAccount : TmpElec.VendorName), 35);
                /*  5 */
                line_out = ErpUtilities.Overlay(line_out, 75, TmpElec.VendorAddress1, 35);
                /*  6 */
                line_out = ErpUtilities.Overlay(line_out, 110, TmpElec.VendorCity, 35);
                /*  7 */
                line_out = ErpUtilities.Overlay(line_out, 145, VendorISOCountryCode, 2);
                /*  8 */
                line_out = ErpUtilities.Overlay(line_out, 147, TmpElec.VendorCountry, 35);
                TotalRecords = TotalRecords + 1;
                lineLevel = lineLevel + 1;
                OutFileLineRow.Line_out = line_out;
                if ((VendorBankISOCountryCode.Compare("NL") == 0 || String.IsNullOrEmpty(VendorBankISOCountryCode)))
                {
                    if (String.IsNullOrEmpty(VendorBankSWIFTAddress))
                    {
                        throw new BLException(GlobalStrings.TheSWIFTAddressOfTheBankOfTheSupplIsRequi(TmpElec.VendorName));
                    }
                    if (String.IsNullOrEmpty(TmpElec.VendorBankName))
                    {
                        throw new BLException(GlobalStrings.TheNameOfTheBankOfTheSupplIsRequi(TmpElec.VendorName));
                    }
                    if (String.IsNullOrEmpty(TmpElec.VendorBankCity))
                    {
                        throw new BLException(GlobalStrings.TheCityOfTheBankOfSupplIsRequi(TmpElec.VendorName));
                    }
                }
                /* 23: batch payment record part 3 */
                OutFileLineRow = new SFCommon.OutFileLine();
                SFCommon.ttOutFileLineRows.Add(OutFileLineRow);
                line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
                /*  1 */
                line_out = ErpUtilities.Overlay(line_out, 0, Compatibility.Convert.ToString(lineLevel), 2);
                /*  2 */
                line_out = ErpUtilities.Overlay(line_out, 2, PaymentNumber, 4);
                /*  3 */
                line_out = ErpUtilities.Overlay(line_out, 6, VendorBankSWIFTAddress, 11);
                /*  4 */
                line_out = ErpUtilities.Overlay(line_out, 17, TmpElec.VendorBankName, 35);
                /*  5 */
                line_out = ErpUtilities.Overlay(line_out, 52, TmpElec.VendorBankAddress1, 35);
                /*  6 */
                line_out = ErpUtilities.Overlay(line_out, 87, TmpElec.VendorBankCity, 35);
                /*  7 */
                line_out = ErpUtilities.Overlay(line_out, 122, VendorBankISOCountryCode, 2);
                /*  8 */
                line_out = ErpUtilities.Overlay(line_out, 124, TmpElec.VendorBankCountry, 35);
                TotalRecords = TotalRecords + 1;
                lineLevel = lineLevel + 1;
                OutFileLineRow.Line_out = line_out;
                /* 24: batch payment record part 4 */
                OutFileLineRow = new SFCommon.OutFileLine();
                SFCommon.ttOutFileLineRows.Add(OutFileLineRow);
                line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
                /*  1 */
                line_out = ErpUtilities.Overlay(line_out, 0, Compatibility.Convert.ToString(lineLevel), 2);
                /*  2 */
                line_out = ErpUtilities.Overlay(line_out, 2, PaymentNumber, 4);
                /*  3 */
                line_out = ErpUtilities.Overlay(line_out, 6, Description_1, 35);
                /*  4 */
                /* not allowed for euro transfer */
                /*  5 */
                /* not allowed for euro transfer */
                /*  6 */
                /* not allowed for euro transfer */
                TotalRecords = TotalRecords + 1;
                lineLevel = lineLevel + 1;
                OutFileLineRow.Line_out = line_out;

                #region >>===== ABL Source ================================>>
                //
                //find TmpCurrTotal where TmpCurrTotal.ISOCurrencyCode = IsoCurrencySymbol exclusive-lock no-error;
                //
                #endregion == ABL Source =================================<<

                ttTmpCurrTotal = (from TmpCurrTotal_Row in ttTmpCurrTotalRows
                                  where TmpCurrTotal_Row.ISOCurrencyCode.Compare(IsoCurrencySymbol) == 0
                                  select TmpCurrTotal_Row).FirstOrDefault();
                if (ttTmpCurrTotal == null)
                {
                    ttTmpCurrTotal = new TmpCurrTotal();
                    ttTmpCurrTotalRows.Add(ttTmpCurrTotal);
                    ttTmpCurrTotal.ISOCurrencyCode = IsoCurrencySymbol;
                    ttTmpCurrTotal.TotalAmount = TmpElec.DocCheckAmt;
                    ttTmpCurrTotal.TotalPayments = 1;
                }
                else
                {
                    ttTmpCurrTotal.TotalAmount = ttTmpCurrTotal.TotalAmount + TmpElec.DocCheckAmt;
                    ttTmpCurrTotal.TotalPayments = ttTmpCurrTotal.TotalPayments + 1;
                }
            }/* for each TmpElec... */

            lineLevel = 31;

            #region >>===== ABL Source ================================>>
            //
            //for each TmpCurrTotal:
            //           
            //                    
            //
            #endregion == ABL Source =================================<<

            foreach (var ttTmpCurrTotal in ttTmpCurrTotalRows)
            {
                TAmount = this.cnvAmount(ttTmpCurrTotal.ISOCurrencyCode, Compatibility.Convert.ToString((ttTmpCurrTotal.TotalAmount * 100), "999999999999999"), 14);
                /* 31: batch currency total record  */
                OutFileLineRow = new SFCommon.OutFileLine();
                SFCommon.ttOutFileLineRows.Add(OutFileLineRow);
                line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
                /*  1 */
                line_out = ErpUtilities.Overlay(line_out, 0, Compatibility.Convert.ToString(lineLevel), 2);
                /*  2 */
                line_out = ErpUtilities.Overlay(line_out, 2, ttTmpCurrTotal.ISOCurrencyCode, 3);
                /*  3 */
                line_out = ErpUtilities.Overlay(line_out, 5, TAmount, 15);
                /*  4 */
                line_out = ErpUtilities.Overlay(line_out, 20, Compatibility.Convert.ToString(ttTmpCurrTotal.TotalPayments, "9999"), 4);
                TotalRecords = TotalRecords + 1;
                lineLevel = lineLevel + 1;
                OutFileLineRow.Line_out = line_out;
            }

            /* 41: Batch Closing record */
            lineLevel = 41;
            /* This code adds information from the specified payment method into a document.
               The document will have the structure like this:

                1 line of file information
                1 batches consisting of
                  x payments consisting of
                      4 payment transaction records
                  y currency total records
                1 line of payment method information
                1 line of file closing information.

            SCR 40680  - uncomment the string below if you want insert payment method information  */
            /*
            create OutFileLine.
            {ei/SoftFormat.i &TableName        = PayMethodProp
                             &EFTHeadUID       = EFTHeadUID
                             &LineOut          = OutFileLine.Line-out
                             &LineLen          = lineLen
                             &LineLevel        = lineLevel
                             &LineLevelFormat  = lineLevelFormat
                             &TotalRecords     = TotalRecords}
            */
            OutFileLineRow = new SFCommon.OutFileLine();
            SFCommon.ttOutFileLineRows.Add(OutFileLineRow);
            line_out = ((lineLen > 0) ? " ".PadRight(lineLen + " ".Length, ' ') : null);
            /*  1 */
            line_out = ErpUtilities.Overlay(line_out, 0, Compatibility.Convert.ToString(lineLevel), 2);
            /*  2 */
            line_out = ErpUtilities.Overlay(line_out, 2, Compatibility.Convert.ToString(TotalRecords + 1, "999999"), 6);
            /*  3 */
            line_out = ErpUtilities.Overlay(line_out, 8, Compatibility.Convert.ToString(TotalOrders, "9999"), 4);
            OutFileLineRow.Line_out = line_out;
        }
        private string cnvAmount(string IsoCurrencySymbol, string Amount, int Alength)
        {
            int old_pos = 0;
            int new_pos = 0;
            string result = string.Empty;
            result = ((Alength > 0) ? "0".PadRight(Alength + "0".Length, '0') : null);
            new_pos = Alength;
            for (old_pos = 1; old_pos <= Amount.Length; old_pos++)
            {
                ErpUtilities.Overlay(result, new_pos, Amount.SubString(Amount.Length + 1 - old_pos - 1, 1), 1);
                new_pos = new_pos - 1;
                if (new_pos == 0)
                {
                    break;
                }
            }

            result = result + "0";
            /* if the currency of the payment is one of these currencies then all
               decimals have to be zero */
            /* Greek Drachme                      */
            if (IsoCurrencySymbol.Compare("BEF") == 0   /* Belgium Francs     (Euro currency) */
            || IsoCurrencySymbol.Compare("ITL") == 0   /* Italian Lira       (Euro currency) */
            || IsoCurrencySymbol.Compare("JPY") == 0   /* Japanese Yen                       */
            || IsoCurrencySymbol.Compare("ESP") == 0   /* Spanish Peseta     (Euro currency) */
            || IsoCurrencySymbol.Compare("PTE") == 0   /* Portuguese Escudos (Euro currency) */
            || IsoCurrencySymbol.Compare("GRD") == 0)
            {
                ErpUtilities.Overlay(result, (Alength - 3), "000", 3);
            }

            return result;  /* Function return value. */
        }

        /*function GetCurrencyID returns character private (input CurrCode as character).
    
        define buffer altCurrency for Currency.
    
        {lib/findtbl.i &QFind = first 
                       &QLock = no-lock 
                       &QTableName = altCurrency 
                       &QWhere = "altCurrency.Company = CUR-COMP
                           and altCurrency.CurrencyCode = CurrCode"
            &COLUMNS = "Company CurrencyCode CurrencyID"}
            
        if (available altCurrency and
            altCurrency.CurrencyID <> "":U) then CurrCode = altCurrency.CurrencyID.
        
        RETURN (CurrCode).*/
        private string GetCurrencyID(string CurrCode)
        {
            var altCurrency = FindFirstCurrency(Session.CompanyID, CurrCode);
            if (altCurrency != null && !string.IsNullOrEmpty(altCurrency.CurrencyID))
            {
                return altCurrency.CurrencyID;
            }
            return string.Empty;
        }
    }
}
