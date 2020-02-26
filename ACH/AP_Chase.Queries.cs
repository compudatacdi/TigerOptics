using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Epicor.Data;
using Erp.Tables;
namespace Erp.Internal.EI
{
    public partial class AP_Chase
    {
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
        private IEnumerable<APTran> SelectAPTran(string company, int headNum)
        {
            if (selectAPTranQuery == null)
            {
                Expression<Func<ErpContext, string, int, IEnumerable<APTran>>> expression =
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

        #region Country Queries
        #region >>===== ABL Source ================================>>
        //
        //FIND first Country WHERE Country.Company = CUR-COMP and Country.CountryNum = Company.CountryNum no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, int, Country> findFirstCountryQuery;
        private Country FindFirstCountry(string company, int countryNum)
        {
            if (findFirstCountryQuery == null)
            {
                Expression<Func<ErpContext, string, int, Country>> expression =
      (ctx, company_ex, countryNum_ex) =>
        (from row in ctx.Country
         where row.Company == company_ex &&
         row.CountryNum == countryNum_ex
         select row).FirstOrDefault();
                findFirstCountryQuery = DBExpressionCompiler.Compile(expression);
            }

            return findFirstCountryQuery(this.Db, company, countryNum);
        }

        #region >>===== ABL Source ================================>>
        //
        //FIND first Country WHERE Country.Company = CUR-COMP and Country.CountryNum = TmpElec.VendorCountryNum no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, int, Country> findFirstCountry2Query;
        private Country FindFirstCountry2(string company, int countryNum)
        {
            if (findFirstCountry2Query == null)
            {
                Expression<Func<ErpContext, string, int, Country>> expression =
      (ctx, company_ex, countryNum_ex) =>
        (from row in ctx.Country
         where row.Company == company_ex &&
         row.CountryNum == countryNum_ex
         select row).FirstOrDefault();
                findFirstCountry2Query = DBExpressionCompiler.Compile(expression);
            }

            return findFirstCountry2Query(this.Db, company, countryNum);
        }

        #region >>===== ABL Source ================================>>
        //
        //FIND first Country WHERE Country.Company = CUR-COMP and Country.CountryNum = TmpElec.VendorBankCountryNum no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, int, Country> findFirstCountry3Query;
        private Country FindFirstCountry3(string company, int countryNum)
        {
            if (findFirstCountry3Query == null)
            {
                Expression<Func<ErpContext, string, int, Country>> expression =
      (ctx, company_ex, countryNum_ex) =>
        (from row in ctx.Country
         where row.Company == company_ex &&
         row.CountryNum == countryNum_ex
         select row).FirstOrDefault();
                findFirstCountry3Query = DBExpressionCompiler.Compile(expression);
            }

            return findFirstCountry3Query(this.Db, company, countryNum);
        }
        #endregion Country Queries

        #region Currency Queries
        #region >>===== ABL Source ================================>>
        //
        //FIND first Currency WHERE Currency.Company = cur-comp and 
        //                              Currency.BaseCurr = true no-lock
        //
        #endregion == ABL Source =================================<<

        static Func<ErpContext, string, bool, Currency> findFirstCurrencyQuery;
        private Currency FindFirstCurrency(string company, bool baseCurr)
        {
            if (findFirstCurrencyQuery == null)
            {
                Expression<Func<ErpContext, string, bool, Currency>> expression =
      (ctx, company_ex, baseCurr_ex) =>
        (from row in ctx.Currency
         where row.Company == company_ex &&
         row.BaseCurr == baseCurr_ex
         select row).FirstOrDefault();
                findFirstCurrencyQuery = DBExpressionCompiler.Compile(expression);
            }

            return findFirstCurrencyQuery(this.Db, company, baseCurr);
        }

        static Func<ErpContext, string, string, Currency> findFirstCurrencyQuery2;
        private Currency FindFirstCurrency(string company, string currCode)
        {
            if (findFirstCurrencyQuery2 == null)
            {
                Expression<Func<ErpContext, string, string, Currency>> expression =
      (ctx, company_ex, CurrCode_ex) =>
        (from row in ctx.Currency
         where row.Company == company_ex &&
         row.CurrencyCode == CurrCode_ex
         select row).FirstOrDefault();
                findFirstCurrencyQuery2 = DBExpressionCompiler.Compile(expression);
            }

            return findFirstCurrencyQuery2(this.Db, company, currCode);
        }
        #endregion Currency Queries
    }
}
