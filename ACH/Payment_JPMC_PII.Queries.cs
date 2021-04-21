using System;
using System.Linq;
using System.Linq.Expressions;
using Epicor.Data;
using Erp.Tables;

//eb7:
using System.Collections.Generic;


namespace Erp.Internal.EI
{
    public partial class Payment_JPMC_PII
    {

		//eb7:
        #region APTran Queries
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

        static Func<ErpContext, string, int, IEnumerable<APTran>> selectAPTranQuery;
//        static Func<ErpContext, string, int,<APTran> selectAPTranQuery;
//        static Func<ErpContext, string, int, APTran> selectAPTranQuery;
//        static Func<ErpContext, string, int, IEnumerable<APTran>> selectAPTranQuery;

        private IEnumerable<APTran> SelectAPTran(string company, int headNum)
//        private APTran SelectAPTran(string company, int headNum)
        {
            if (selectAPTranQuery == null)
            {
                Expression<Func<ErpContext, string, int, IEnumerable<APTran>>> expression =
//                Expression<Func<ErpContext, string, int, APTran>> expression =
      (ctx, company_ex, headNum_ex) =>
        (from row in ctx.APTran
         where row.Company == company_ex &&
         row.HeadNum == headNum_ex
         orderby row.Company, row.HeadNum, row.APTranNo, row.InvoiceNum
         select row);
                selectAPTranQuery = DBExpressionCompiler.Compile(expression);
            }

            return selectAPTranQuery(this.Db, company, headNum);
        }
        #endregion APTran Queries
		
		
        #region BankAcct Queries
        #region >>===== ABL Source ================================>>
        //
        //FIND first BankAcct WHERE BankAcct.Company = Cur-comp and BankAcct.BankAcctID = TmpElec.FromBankAcctID no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, string, BankAcct> findFirstBankAcctQuery;
        private BankAcct FindFirstBankAcct(string company, string bankAcctID)
        {
            if (findFirstBankAcctQuery == null)
            {
                Expression<Func<ErpContext, string, string, BankAcct>> expression =
      (ctx, company_ex, bankAcctID_ex) =>
        (from row in ctx.BankAcct
         where row.Company == company_ex &&
         row.BankAcctID == bankAcctID_ex
         select row).FirstOrDefault();
                findFirstBankAcctQuery = DBExpressionCompiler.Compile(expression);
            }

            return findFirstBankAcctQuery(this.Db, company, bankAcctID);
        }
        #endregion BankAcct Queries

        #region Company Queries
        #region >>===== ABL Source ================================>>
        //
        //FIND first Company WHERE Company.Company = CUR-COMP no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, Company> findFirstCompanyQuery;
        private Company FindFirstCompany(string company1)
        {
            if (findFirstCompanyQuery == null)
            {
                Expression<Func<ErpContext, string, Company>> expression =
      (ctx, company1_ex) =>
        (from row in ctx.Company
         where row.Company1 == company1_ex
         select row).FirstOrDefault();
                findFirstCompanyQuery = DBExpressionCompiler.Compile(expression);
            }

            return findFirstCompanyQuery(this.Db, company1);
        }
        #endregion Company Queries

        #region TmpElec Queries
        #region >>===== ABL Source ================================>>
        //
        //find first TmpElec no-lock no-error;
        //
        #endregion == ABL Source =================================<<

      //  static Func<ErpContext, TmpElec> findFirstTmpElecQuery;
      //  private TmpElec FindFirstTmpElec()
      //  {
      //      if (findFirstTmpElecQuery == null)
      //      {
      //          Expression<Func<ErpContext, TmpElec>> expression =
      //(ctx) =>
      //  (from row in ctx.TmpElec
      //   select row).FirstOrDefault();
      //          findFirstTmpElecQuery = DBExpressionCompiler.Compile(expression);
      //      }

      //      return findFirstTmpElecQuery(this.Db);
      //  }
        #endregion TmpElec Queries

        #region VendBank Queries
        #region >>===== ABL Source ================================>>
        //
        //FIND first VendBank WHERE VendBank.Company = cur-comp and
        //                                  VendBank.VendorNum = TmpElec.VendorNum and
        //                                  VendBank.BankID = TmpElec.VendorBankID  no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, int, string, VendBank> findFirstVendBankQuery;
        private VendBank FindFirstVendBank(string company, int vendorNum, string bankID)
        {
            if (findFirstVendBankQuery == null)
            {
                Expression<Func<ErpContext, string, int, string, VendBank>> expression =
      (ctx, company_ex, vendorNum_ex, bankID_ex) =>
        (from row in ctx.VendBank
         where row.Company == company_ex &&
         row.VendorNum == vendorNum_ex &&
         row.BankID == bankID_ex
         select row).FirstOrDefault();
                findFirstVendBankQuery = DBExpressionCompiler.Compile(expression);
            }

            return findFirstVendBankQuery(this.Db, company, vendorNum, bankID);
        }
        #endregion VendBank Queries
    }
}
