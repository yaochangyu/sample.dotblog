using OpenQA.Selenium;
using OpenQA.Selenium.Support.PageObjects;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;

public class RegisterPage
{
    private Dictionary<string, string> data;
    private IWebDriver driver;
    private int timeout = 15;

    [FindsBy(How = How.CssSelector, Using = "a[href='/about']")]
    [CacheLookup]
    private IWebElement AboutUs;

    [FindsBy(How = How.CssSelector, Using = "a.optanon-allow-all")]
    [CacheLookup]
    private IWebElement AcceptCookies;

    [FindsBy(How = How.CssSelector, Using = "button.TK-Products-Menu-Item-Button")]
    [CacheLookup]
    private IWebElement AllProducts;

    [FindsBy(How = How.CssSelector, Using = "#optanon-popup-bottom div:nth-of-type(2) div:nth-of-type(2) a")]
    [CacheLookup]
    private IWebElement AllowAll;

    [FindsBy(How = How.CssSelector, Using = "a[href='tel:+61398058670']")]
    [CacheLookup]
    private IWebElement Australia61398058670;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(7) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Awards1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(7) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Awards2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(3) ul.TK-Footer-List li:nth-of-type(4) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement Awards3;

    [FindsBy(How = How.CssSelector, Using = "a.Panel-switch.Link--prev.Link--s.Text--xl.u-ff-sans1.u-mt3.js-back-to-login")]
    [CacheLookup]
    private IWebElement BackToLogin1;

    [FindsBy(How = How.CssSelector, Using = "a.Link--prev.Link--s.Text--xl.u-ff-sans1.u-mt3.js-toggle-loginpanel")]
    [CacheLookup]
    private IWebElement BackToLogin2;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer ul:nth-of-type(1) li:nth-of-type(3) a.TK-Menu-Item-Link")]
    [CacheLookup]
    private IWebElement Blogs1;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(2) ul.TK-Footer-List li:nth-of-type(5) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement Blogs2;

    [FindsBy(How = How.CssSelector, Using = "a[href='tel:+35928099850']")]
    [CacheLookup]
    private IWebElement Bulgaria35928099850;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(10) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Careers1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(10) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Careers2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(4) ul.TK-Footer-List li:nth-of-type(5) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement Careers3;

    [FindsBy(How = How.CssSelector, Using = "#telerik div:nth-of-type(6) div:nth-of-type(2) div:nth-of-type(4) div:nth-of-type(1) div.optanon-alert-box-button-middle a.optanon-alert-box-close")]
    [CacheLookup]
    private IWebElement Close;

    [FindsBy(How = How.Id, Using = "js-tlrk-nav-overlay")]
    [CacheLookup]
    private IWebElement CloseMobileMenu;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Medium.TK-Dropdown--White li:nth-of-type(2) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement CognitiveServices1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(2) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement CognitiveServices2;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) a.TK-Hat-Menu-Link")]
    [CacheLookup]
    private IWebElement Company1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) a.TK-Aside-Menu-Link")]
    [CacheLookup]
    private IWebElement Company2;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_tbCompanyName")]
    [CacheLookup]
    private IWebElement Company3;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(1) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement CompanyOverview1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(1) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement CompanyOverview2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(2) div.TK-col-24 ul.TK-Footer-List li.TK-Footer-List-Item a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement ContactSales1;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(3) div:nth-of-type(3) ul.TK-Footer-List li:nth-of-type(2) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement ContactSales2;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_NoBotValidation_ctl00_ctl00_tbUserField")]
    [CacheLookup]
    private IWebElement ContactSupportIfTheProblemPersists;

    [FindsBy(How = How.CssSelector, Using = "a[title='Contact Us']")]
    [CacheLookup]
    private IWebElement ContactUs;

    [FindsBy(How = How.CssSelector, Using = "a.optanon-toggle-display")]
    [CacheLookup]
    private IWebElement CookieSettings;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(8) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement CorporateBlog1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(8) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement CorporateBlog2;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_ddlCountry")]
    [CacheLookup]
    private IWebElement Country;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_btnSubmit")]
    [CacheLookup]
    private IWebElement CreateAccount;

    [FindsBy(How = How.CssSelector, Using = "a.Link--next.Text--xl.Link--s.u-ff-sans1.js-toggle-signup.u-pr2")]
    [CacheLookup]
    private IWebElement CreateAnAccountForFree;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(9) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Customers1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(9) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Customers2;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Medium.TK-Dropdown--White li:nth-of-type(3) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement DataConnectivityAndIntegration1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(3) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement DataConnectivityAndIntegration2;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer ul:nth-of-type(1) li:nth-of-type(1) a.TK-Menu-Item-Link")]
    [CacheLookup]
    private IWebElement Demos1;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(2) ul.TK-Footer-List li:nth-of-type(1) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement Demos2;

    [FindsBy(How = How.CssSelector, Using = "a.TK-Bundle")]
    [CacheLookup]
    private IWebElement DevcraftallTelerikNetToolsAndKendo;

    [FindsBy(How = How.CssSelector, Using = "a[href='/blogs/developer-central']")]
    [CacheLookup]
    private IWebElement DeveloperCentral;

    [FindsBy(How = How.CssSelector, Using = "a[href='/support']")]
    [CacheLookup]
    private IWebElement DocsSupport;

    [FindsBy(How = How.CssSelector, Using = "a[href='/documentation']")]
    [CacheLookup]
    private IWebElement Documentation;

    [FindsBy(How = How.CssSelector, Using = "a[title='Make a small contribution']")]
    [CacheLookup]
    private IWebElement Donate;

    [FindsBy(How = How.Id, Using = "username")]
    [CacheLookup]
    private IWebElement Email1;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_tbEmail")]
    [CacheLookup]
    private IWebElement Email2;

    [FindsBy(How = How.Id, Using = "GeneralContent_C047_ctl00_ctl00_mailTextBox")]
    [CacheLookup]
    private IWebElement EmailForYourTelerikAccount;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(6) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Events1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(6) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Events2;

    [FindsBy(How = How.CssSelector, Using = "a[href='/events']")]
    [CacheLookup]
    private IWebElement Events3;

    [FindsBy(How = How.Id, Using = "Facebook")]
    [CacheLookup]
    private IWebElement Facebook;

    [FindsBy(How = How.CssSelector, Using = "a[title='ImTranslator Feedback']")]
    [CacheLookup]
    private IWebElement Feedback;

    [FindsBy(How = How.CssSelector, Using = "a[href='/fiddler']")]
    [CacheLookup]
    private IWebElement Fiddler;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_tbFirstName")]
    [CacheLookup]
    private IWebElement FirstName;

    [FindsBy(How = How.CssSelector, Using = "a.Panel-switch.js-toggle-forgot.u-ff-sans0")]
    [CacheLookup]
    private IWebElement ForgotIt;

    [FindsBy(How = How.CssSelector, Using = "a[href='/forums']")]
    [CacheLookup]
    private IWebElement Forums;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(1) ul.TK-Footer-List li:nth-of-type(1) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement FreeTrials;

    [FindsBy(How = How.CssSelector, Using = "#optanon-menu li:nth-of-type(4) p a")]
    [CacheLookup]
    private IWebElement FunctionalCookies;

    [FindsBy(How = How.CssSelector, Using = "a.TK-Button.TK-Button--CTA")]
    [CacheLookup]
    private IWebElement GetAFreeTrial;

    [FindsBy(How = How.Id, Using = "Google")]
    [CacheLookup]
    private IWebElement Google;

    [FindsBy(How = How.CssSelector, Using = "#alreadyAgreedLabel a")]
    [CacheLookup]
    private IWebElement Here1;

    [FindsBy(How = How.CssSelector, Using = "#customerLabel a")]
    [CacheLookup]
    private IWebElement Here2;

    [FindsBy(How = How.CssSelector, Using = "a[title='Translation History']")]
    [CacheLookup]
    private IWebElement History;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_euCanadaCheckboxControl_ctl00_ctl00_iAgreeCheckbox")]
    [CacheLookup]
    private IWebElement IAgreeToReceiveEmailCommunications;

    [FindsBy(How = How.Id, Using = "chkMain")]
    [CacheLookup]
    private IWebElement Inactive;

    [FindsBy(How = How.CssSelector, Using = "a[href='tel:+911244300987']")]
    [CacheLookup]
    private IWebElement India911244300987;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(3) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement InvestorRelations1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(3) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement InvestorRelations2;

    [FindsBy(How = How.CssSelector, Using = "a[href='/justassembly']")]
    [CacheLookup]
    private IWebElement Justassembly;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/decompiler.aspx']")]
    [CacheLookup]
    private IWebElement Justdecompile;

    [FindsBy(How = How.Id, Using = "RememberMe")]
    [CacheLookup]
    private IWebElement KeepMeLoggedIn;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-dash div.TK-container div.TK-row.TK-BG div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(1) div:nth-of-type(1) a:nth-of-type(1)")]
    [CacheLookup]
    private IWebElement KendoUi1;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(1) div:nth-of-type(2) a.TK-Footer-Featured-Link")]
    [CacheLookup]
    private IWebElement KendoUi2;

    [FindsBy(How = How.CssSelector, Using = "a[href='/about/customers']")]
    [CacheLookup]
    private IWebElement KeyCustomers;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_tbLastName")]
    [CacheLookup]
    private IWebElement LastName;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(2) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Leadership1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(2) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Leadership2;

    [FindsBy(How = How.Id, Using = "LiveID")]
    [CacheLookup]
    private IWebElement Liveid;

    [FindsBy(How = How.Id, Using = "GeneralContent_C048_ctl00_ctl00_LoginButton")]
    [CacheLookup]
    private IWebElement LogIn;

    [FindsBy(How = How.CssSelector, Using = "a[title='Your Account']")]
    [CacheLookup]
    private IWebElement Login;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(4) ul.TK-Footer-List li:nth-of-type(3) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement MediaCoverage;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Medium.TK-Dropdown--White li:nth-of-type(1) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement MobilityAndHighProductivityAppDev1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(1) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement MobilityAndHighProductivityAppDev2;

    [FindsBy(How = How.CssSelector, Using = "#optanon-menu li:nth-of-type(6) p a")]
    [CacheLookup]
    private IWebElement MoreInformation;

    [FindsBy(How = How.CssSelector, Using = "a[href='https://www.progress.com/nativescript']")]
    [CacheLookup]
    private IWebElement NativescriptOssFramework;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(11) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Offices1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(11) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Offices2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(4) ul.TK-Footer-List li:nth-of-type(4) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement Offices3;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Medium.TK-Dropdown--White li:nth-of-type(6) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Openedge1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(6) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement Openedge2;

    [FindsBy(How = How.CssSelector, Using = "a[title='Show options']")]
    [CacheLookup]
    private IWebElement Options;

    private readonly string PageLoadedText = "Progress, Telerik, and certain product names used herein are trademarks or registered trademarks of Progress Software Corporation and/or one of its subsidiaries or affiliates in the U";

    private readonly string PageUrl = "/login/v2/telerik#register";

    [FindsBy(How = How.CssSelector, Using = "a[href='https://www.progress.com/partners/partner-directory']")]
    [CacheLookup]
    private IWebElement Partners1;

    [FindsBy(How = How.CssSelector, Using = "a[href='https://partnerlink.progress.com']")]
    [CacheLookup]
    private IWebElement Partners2;

    [FindsBy(How = How.Id, Using = "password")]
    [CacheLookup]
    private IWebElement Password;

    [FindsBy(How = How.CssSelector, Using = "#optanon-menu li:nth-of-type(3) p a")]
    [CacheLookup]
    private IWebElement PerformanceCookies;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_tbPhone")]
    [CacheLookup]
    private IWebElement Phone;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(5) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement PressCoverage1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(5) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement PressCoverage2;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Small.TK-Dropdown--White li:nth-of-type(4) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement PressReleases1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(1) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(4) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement PressReleases2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(4) ul.TK-Footer-List li:nth-of-type(2) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement PressReleases3;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer ul:nth-of-type(1) li:nth-of-type(2) a.TK-Menu-Item-Link")]
    [CacheLookup]
    private IWebElement Pricing1;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(1) ul.TK-Footer-List li:nth-of-type(2) a.TK-Footer-Link")]
    [CacheLookup]
    private IWebElement Pricing2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(3) div:nth-of-type(2) ul.TK-Footer-List-Horizontal li:nth-of-type(3) a.TK-Footer-Link-Tiny")]
    [CacheLookup]
    private IWebElement PrivacyCenter;

    [FindsBy(How = How.CssSelector, Using = "a[href='https://www.progress.com/legal/privacy-policy']")]
    [CacheLookup]
    private IWebElement PrivacyPolicy;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(3) div:nth-of-type(2) p.TK-Footer-Power a.TK-Footer-Link-Tiny")]
    [CacheLookup]
    private IWebElement ProgressSitefinity;

    [FindsBy(How = How.CssSelector, Using = "a[href='/support/whats-new/release-history']")]
    [CacheLookup]
    private IWebElement ReleaseHistory;

    [FindsBy(How = How.CssSelector, Using = "#optanon-popup-bottom div:nth-of-type(1) div:nth-of-type(2) a")]
    [CacheLookup]
    private IWebElement SaveSettings;

    [FindsBy(How = How.CssSelector, Using = "a[title='Search']")]
    [CacheLookup]
    private IWebElement Search;

    [FindsBy(How = How.Id, Using = "GeneralContent_C047_ctl00_ctl00_sendRecoveryMailBtn")]
    [CacheLookup]
    private IWebElement Send;

    [FindsBy(How = How.CssSelector, Using = "a.TK-Aside-Menu-Link.js-TK-Nav-Cart-Link")]
    [CacheLookup]
    private IWebElement ShoppingCart0;

    [FindsBy(How = How.CssSelector, Using = "a[href='/company/feedback']")]
    [CacheLookup]
    private IWebElement SiteFeedback;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-dash div.TK-container div.TK-row.TK-BG div:nth-of-type(2) div:nth-of-type(1) div:nth-of-type(2) div:nth-of-type(4) a.TK-Dash-Link")]
    [CacheLookup]
    private IWebElement Sitefinity;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_stateScriptControl_ctl00_ctl00_ddlState")]
    [CacheLookup]
    private IWebElement Stateprovince;

    [FindsBy(How = How.CssSelector, Using = "#optanon-menu li:nth-of-type(2) p a")]
    [CacheLookup]
    private IWebElement StrictlyNecessaryCookies;

    [FindsBy(How = How.CssSelector, Using = "a[href='/about/success-stories']")]
    [CacheLookup]
    private IWebElement SuccessStories;

    [FindsBy(How = How.CssSelector, Using = "#optanon-menu li:nth-of-type(5) p a")]
    [CacheLookup]
    private IWebElement TargetingCookies;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) a.TK-Hat-Menu-Link")]
    [CacheLookup]
    private IWebElement Technology1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) a.TK-Aside-Menu-Link")]
    [CacheLookup]
    private IWebElement Technology2;

    [FindsBy(How = How.CssSelector, Using = "#GeneralContent_T73A12E0A120_Col00 div:nth-of-type(2) footer.TK-Footer div.TK-container div:nth-of-type(1) div:nth-of-type(1) div:nth-of-type(1) a.TK-Footer-Featured-Link")]
    [CacheLookup]
    private IWebElement TelerikDevcraft;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/mocking.aspx']")]
    [CacheLookup]
    private IWebElement TelerikJustmock;

    [FindsBy(How = How.CssSelector, Using = "a[href='/report-server']")]
    [CacheLookup]
    private IWebElement TelerikReportServer;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/reporting.aspx']")]
    [CacheLookup]
    private IWebElement TelerikReporting;

    [FindsBy(How = How.CssSelector, Using = "a[href='/company/terms-of-use']")]
    [CacheLookup]
    private IWebElement TermsOfUse;

    [FindsBy(How = How.CssSelector, Using = "a[href='/teststudio']")]
    [CacheLookup]
    private IWebElement TestStudio;

    [FindsBy(How = How.CssSelector, Using = "a[href='/teststudio-dev']")]
    [CacheLookup]
    private IWebElement TestStudioDevEdition;

    [FindsBy(How = How.CssSelector, Using = "a[href='/about/testimonials']")]
    [CacheLookup]
    private IWebElement Testimonials;

    [FindsBy(How = How.CssSelector, Using = "a[href='/teststudio/testing-framework']")]
    [CacheLookup]
    private IWebElement TestingFramework;

    [FindsBy(How = How.Id, Using = "SL_locer")]
    [CacheLookup]
    private IWebElement TexttospeechFunctionIsLimitedTo2001;

    [FindsBy(How = How.Id, Using = "SL_lng_from")]
    [CacheLookup]
    private IWebElement TexttospeechFunctionIsLimitedTo2002;

    [FindsBy(How = How.Id, Using = "SL_lng_to")]
    [CacheLookup]
    private IWebElement TexttospeechFunctionIsLimitedTo2003;

    [FindsBy(How = How.Id, Using = "SL_BBL_locer")]
    [CacheLookup]
    private IWebElement TexttospeechFunctionIsLimitedTo2004;

    [FindsBy(How = How.Id, Using = "GeneralContent_C067_ctl00_ctl00_euCanadaCheckboxControl_ctl00_ctl00_emptyValueCheckbox")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest1;

    [FindsBy(How = How.Id, Using = "dnbCountry")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest2;

    [FindsBy(How = How.Id, Using = "dnb_duns")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest3;

    [FindsBy(How = How.Id, Using = "db_primary_sic")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest4;

    [FindsBy(How = How.Id, Using = "db_sic_desc")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest5;

    [FindsBy(How = How.Id, Using = "db_primary_naics")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest6;

    [FindsBy(How = How.Id, Using = "db_naics_desc")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest7;

    [FindsBy(How = How.Id, Using = "db_annual_sales")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest8;

    [FindsBy(How = How.Id, Using = "db_employee_count")]
    [CacheLookup]
    private IWebElement ThankYouForYourContinuedInterest9;

    [FindsBy(How = How.CssSelector, Using = "a[href='https://www.progress.com/legal/trademarks']")]
    [CacheLookup]
    private IWebElement Trademarks;

    [FindsBy(How = How.CssSelector, Using = "a[href='/kendo-angular-ui']")]
    [CacheLookup]
    private IWebElement UiForAngular;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/aspnet-ajax.aspx']")]
    [CacheLookup]
    private IWebElement UiForAspNetAjax;

    [FindsBy(How = How.CssSelector, Using = "a[href='/aspnet-core-ui']")]
    [CacheLookup]
    private IWebElement UiForAspNetCore;

    [FindsBy(How = How.CssSelector, Using = "a[href='/aspnet-mvc']")]
    [CacheLookup]
    private IWebElement UiForAspNetMvc;

    [FindsBy(How = How.CssSelector, Using = "a[href='/blazor-ui']")]
    [CacheLookup]
    private IWebElement UiForBlazor;

    [FindsBy(How = How.CssSelector, Using = "a[href='/kendo-jquery-ui']")]
    [CacheLookup]
    private IWebElement UiForJquery;

    [FindsBy(How = How.CssSelector, Using = "a[href='/jsp-ui']")]
    [CacheLookup]
    private IWebElement UiForJsp;

    [FindsBy(How = How.CssSelector, Using = "a[href='/php-ui']")]
    [CacheLookup]
    private IWebElement UiForPhp;

    [FindsBy(How = How.CssSelector, Using = "a[href='/kendo-react-ui']")]
    [CacheLookup]
    private IWebElement UiForReact;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/silverlight/overview.aspx']")]
    [CacheLookup]
    private IWebElement UiForSilverlight;

    [FindsBy(How = How.CssSelector, Using = "a[href='/universal-windows-platform-ui']")]
    [CacheLookup]
    private IWebElement UiForUwp;

    [FindsBy(How = How.CssSelector, Using = "a[href='/kendo-vue-ui']")]
    [CacheLookup]
    private IWebElement UiForVue;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/winforms.aspx']")]
    [CacheLookup]
    private IWebElement UiForWinforms;

    [FindsBy(How = How.CssSelector, Using = "a[href='/products/wpf/overview.aspx']")]
    [CacheLookup]
    private IWebElement UiForWpf;

    [FindsBy(How = How.CssSelector, Using = "a[href='/xamarin-ui']")]
    [CacheLookup]
    private IWebElement UiForXamarin;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Medium.TK-Dropdown--White li:nth-of-type(4) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement UiTools1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(4) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement UiTools2;

    [FindsBy(How = How.CssSelector, Using = "a[href='tel:+441344360444']")]
    [CacheLookup]
    private IWebElement Uk441344360444;

    [FindsBy(How = How.CssSelector, Using = "a.TK-Dash-Link.TK-New")]
    [CacheLookup]
    private IWebElement UniteUx;

    [FindsBy(How = How.CssSelector, Using = "a[href='tel:+18883652779']")]
    [CacheLookup]
    private IWebElement Usa18883652779;

    [FindsBy(How = How.CssSelector, Using = "a[href='http://converter.telerik.com']")]
    [CacheLookup]
    private IWebElement VbNetToCConverter;

    [FindsBy(How = How.CssSelector, Using = "a.TK-Dash-Featured-Link")]
    [CacheLookup]
    private IWebElement ViewAllProducts;

    [FindsBy(How = How.CssSelector, Using = "a[href='/support/virtual-classroom']")]
    [CacheLookup]
    private IWebElement VirtualClassroom;

    [FindsBy(How = How.CssSelector, Using = "a[href='/ar-vr-lab']")]
    [CacheLookup]
    private IWebElement VrDataviz;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-progress-menu ul.TK-Hat-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Medium.TK-Dropdown--White li:nth-of-type(5) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement WebContentManagement1;

    [FindsBy(How = How.CssSelector, Using = "#js-tlrk-nav-drawer div.TK-Progress-Menu.TK--Mobile ul.TK-Aside-Menu li:nth-of-type(2) ul.TK-Dropdown.TK-Dropdown--Mobile.TK-Dropdown--White li:nth-of-type(5) a.TK-Dropdown-Link")]
    [CacheLookup]
    private IWebElement WebContentManagement2;

    [FindsBy(How = How.Id, Using = "Yahoo")]
    [CacheLookup]
    private IWebElement Yahoo;

    [FindsBy(How = How.CssSelector, Using = "#optanon-menu li:nth-of-type(1) p a")]
    [CacheLookup]
    private IWebElement YourPrivacy;

    public RegisterPage()
        : this(default(IWebDriver), new Dictionary<string, string>(), 15)
    {
    }

    public RegisterPage(IWebDriver driver)
        : this(driver, new Dictionary<string, string>(), 15)
    {
    }

    public RegisterPage(IWebDriver driver, Dictionary<string, string> data)
        : this(driver, data, 15)
    {
    }

    public RegisterPage(IWebDriver driver, Dictionary<string, string> data, int timeout)
    {
        this.driver = driver;
        this.data = data;
        this.timeout = timeout;
    }

    /// <summary>
    /// Click on About Us Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAboutUsLink() 
    {
        AboutUs.Click();
        return this;
    }

    /// <summary>
    /// Click on Accept Cookies Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAcceptCookiesLink() 
    {
        AcceptCookies.Click();
        return this;
    }

    /// <summary>
    /// Click on All Products Button.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAllProductsButton() 
    {
        AllProducts.Click();
        return this;
    }

    /// <summary>
    /// Click on Allow All Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAllowAllLink() 
    {
        AllowAll.Click();
        return this;
    }

    /// <summary>
    /// Click on Australia 61 3 9805 8670 Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAustralia61398058670Link() 
    {
        Australia61398058670.Click();
        return this;
    }

    /// <summary>
    /// Click on Awards Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAwards1Link() 
    {
        Awards1.Click();
        return this;
    }

    /// <summary>
    /// Click on Awards Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAwards2Link() 
    {
        Awards2.Click();
        return this;
    }

    /// <summary>
    /// Click on Awards Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickAwards3Link() 
    {
        Awards3.Click();
        return this;
    }

    /// <summary>
    /// Click on Back To Login Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickBackToLogin1Link() 
    {
        BackToLogin1.Click();
        return this;
    }

    /// <summary>
    /// Click on Back To Login Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickBackToLogin2Link() 
    {
        BackToLogin2.Click();
        return this;
    }

    /// <summary>
    /// Click on Blogs Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickBlogs1Link() 
    {
        Blogs1.Click();
        return this;
    }

    /// <summary>
    /// Click on Blogs Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickBlogs2Link() 
    {
        Blogs2.Click();
        return this;
    }

    /// <summary>
    /// Click on Bulgaria 359 2 8099850 Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickBulgaria35928099850Link() 
    {
        Bulgaria35928099850.Click();
        return this;
    }

    /// <summary>
    /// Click on Careers Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCareers1Link() 
    {
        Careers1.Click();
        return this;
    }

    /// <summary>
    /// Click on Careers Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCareers2Link() 
    {
        Careers2.Click();
        return this;
    }

    /// <summary>
    /// Click on Careers Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCareers3Link() 
    {
        Careers3.Click();
        return this;
    }

    /// <summary>
    /// Click on Close Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCloseLink() 
    {
        Close.Click();
        return this;
    }

    /// <summary>
    /// Click on Close Mobile Menu Button.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCloseMobileMenuButton() 
    {
        CloseMobileMenu.Click();
        return this;
    }

    /// <summary>
    /// Click on Cognitive Services Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCognitiveServices1Link() 
    {
        CognitiveServices1.Click();
        return this;
    }

    /// <summary>
    /// Click on Cognitive Services Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCognitiveServices2Link() 
    {
        CognitiveServices2.Click();
        return this;
    }

    /// <summary>
    /// Click on Company Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCompany1Link() 
    {
        Company1.Click();
        return this;
    }

    /// <summary>
    /// Click on Company Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCompany2Link() 
    {
        Company2.Click();
        return this;
    }

    /// <summary>
    /// Click on Company Overview Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCompanyOverview1Link() 
    {
        CompanyOverview1.Click();
        return this;
    }

    /// <summary>
    /// Click on Company Overview Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCompanyOverview2Link() 
    {
        CompanyOverview2.Click();
        return this;
    }

    /// <summary>
    /// Click on Contact Sales Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickContactSales1Link() 
    {
        ContactSales1.Click();
        return this;
    }

    /// <summary>
    /// Click on Contact Sales Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickContactSales2Link() 
    {
        ContactSales2.Click();
        return this;
    }

    /// <summary>
    /// Click on Contact Us Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickContactUsLink() 
    {
        ContactUs.Click();
        return this;
    }

    /// <summary>
    /// Click on Cookie Settings Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCookieSettingsLink() 
    {
        CookieSettings.Click();
        return this;
    }

    /// <summary>
    /// Click on Corporate Blog Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCorporateBlog1Link() 
    {
        CorporateBlog1.Click();
        return this;
    }

    /// <summary>
    /// Click on Corporate Blog Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCorporateBlog2Link() 
    {
        CorporateBlog2.Click();
        return this;
    }

    /// <summary>
    /// Click on Create Account Button.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCreateAccountButton() 
    {
        CreateAccount.Click();
        return this;
    }

    /// <summary>
    /// Click on Create An Account For Free Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCreateAnAccountForFreeLink() 
    {
        CreateAnAccountForFree.Click();
        return this;
    }

    /// <summary>
    /// Click on Customers Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCustomers1Link() 
    {
        Customers1.Click();
        return this;
    }

    /// <summary>
    /// Click on Customers Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickCustomers2Link() 
    {
        Customers2.Click();
        return this;
    }

    /// <summary>
    /// Click on Data Connectivity And Integration Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDataConnectivityAndIntegration1Link() 
    {
        DataConnectivityAndIntegration1.Click();
        return this;
    }

    /// <summary>
    /// Click on Data Connectivity And Integration Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDataConnectivityAndIntegration2Link() 
    {
        DataConnectivityAndIntegration2.Click();
        return this;
    }

    /// <summary>
    /// Click on Demos Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDemos1Link() 
    {
        Demos1.Click();
        return this;
    }

    /// <summary>
    /// Click on Demos Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDemos2Link() 
    {
        Demos2.Click();
        return this;
    }

    /// <summary>
    /// Click on Devcraftall Telerik .Net Tools And Kendo Ui Javascript Components In One Package. Now Enhanced Withconversational Uionline Trainingdocument Processing Library Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDevcraftallTelerikNetToolsAndKendoLink() 
    {
        DevcraftallTelerikNetToolsAndKendo.Click();
        return this;
    }

    /// <summary>
    /// Click on Developer Central Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDeveloperCentralLink() 
    {
        DeveloperCentral.Click();
        return this;
    }

    /// <summary>
    /// Click on Docs Support Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDocsSupportLink() 
    {
        DocsSupport.Click();
        return this;
    }

    /// <summary>
    /// Click on Documentation Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDocumentationLink() 
    {
        Documentation.Click();
        return this;
    }

    /// <summary>
    /// Click on Donate Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickDonateLink() 
    {
        Donate.Click();
        return this;
    }

    /// <summary>
    /// Click on Events Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickEvents1Link() 
    {
        Events1.Click();
        return this;
    }

    /// <summary>
    /// Click on Events Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickEvents2Link() 
    {
        Events2.Click();
        return this;
    }

    /// <summary>
    /// Click on Events Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickEvents3Link() 
    {
        Events3.Click();
        return this;
    }

    /// <summary>
    /// Click on Facebook Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickFacebookLink() 
    {
        Facebook.Click();
        return this;
    }

    /// <summary>
    /// Click on Feedback Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickFeedbackLink() 
    {
        Feedback.Click();
        return this;
    }

    /// <summary>
    /// Click on Fiddler Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickFiddlerLink() 
    {
        Fiddler.Click();
        return this;
    }

    /// <summary>
    /// Click on Forgot It Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickForgotItLink() 
    {
        ForgotIt.Click();
        return this;
    }

    /// <summary>
    /// Click on Forums Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickForumsLink() 
    {
        Forums.Click();
        return this;
    }

    /// <summary>
    /// Click on Free Trials Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickFreeTrialsLink() 
    {
        FreeTrials.Click();
        return this;
    }

    /// <summary>
    /// Click on Functional Cookies Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickFunctionalCookiesLink() 
    {
        FunctionalCookies.Click();
        return this;
    }

    /// <summary>
    /// Click on Get A Free Trial Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickGetAFreeTrialLink() 
    {
        GetAFreeTrial.Click();
        return this;
    }

    /// <summary>
    /// Click on Google Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickGoogleLink() 
    {
        Google.Click();
        return this;
    }

    /// <summary>
    /// Click on Here Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickHere1Link() 
    {
        Here1.Click();
        return this;
    }

    /// <summary>
    /// Click on Here Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickHere2Link() 
    {
        Here2.Click();
        return this;
    }

    /// <summary>
    /// Click on History Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickHistoryLink() 
    {
        History.Click();
        return this;
    }

    /// <summary>
    /// Click on India 91 124 4300987 Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickIndia911244300987Link() 
    {
        India911244300987.Click();
        return this;
    }

    /// <summary>
    /// Click on Investor Relations Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickInvestorRelations1Link() 
    {
        InvestorRelations1.Click();
        return this;
    }

    /// <summary>
    /// Click on Investor Relations Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickInvestorRelations2Link() 
    {
        InvestorRelations2.Click();
        return this;
    }

    /// <summary>
    /// Click on Justassembly Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickJustassemblyLink() 
    {
        Justassembly.Click();
        return this;
    }

    /// <summary>
    /// Click on Justdecompile Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickJustdecompileLink() 
    {
        Justdecompile.Click();
        return this;
    }

    /// <summary>
    /// Click on Kendo Ui Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickKendoUi1Link() 
    {
        KendoUi1.Click();
        return this;
    }

    /// <summary>
    /// Click on Kendo Ui Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickKendoUi2Link() 
    {
        KendoUi2.Click();
        return this;
    }

    /// <summary>
    /// Click on Key Customers Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickKeyCustomersLink() 
    {
        KeyCustomers.Click();
        return this;
    }

    /// <summary>
    /// Click on Leadership Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickLeadership1Link() 
    {
        Leadership1.Click();
        return this;
    }

    /// <summary>
    /// Click on Leadership Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickLeadership2Link() 
    {
        Leadership2.Click();
        return this;
    }

    /// <summary>
    /// Click on Liveid Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickLiveidLink() 
    {
        Liveid.Click();
        return this;
    }

    /// <summary>
    /// Click on Log In Button.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickLogInButton() 
    {
        LogIn.Click();
        return this;
    }

    /// <summary>
    /// Click on Login Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickLoginLink() 
    {
        Login.Click();
        return this;
    }

    /// <summary>
    /// Click on Media Coverage Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickMediaCoverageLink() 
    {
        MediaCoverage.Click();
        return this;
    }

    /// <summary>
    /// Click on Mobility And High Productivity App Dev Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickMobilityAndHighProductivityAppDev1Link() 
    {
        MobilityAndHighProductivityAppDev1.Click();
        return this;
    }

    /// <summary>
    /// Click on Mobility And High Productivity App Dev Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickMobilityAndHighProductivityAppDev2Link() 
    {
        MobilityAndHighProductivityAppDev2.Click();
        return this;
    }

    /// <summary>
    /// Click on More Information Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickMoreInformationLink() 
    {
        MoreInformation.Click();
        return this;
    }

    /// <summary>
    /// Click on Nativescript Oss Framework Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickNativescriptOssFrameworkLink() 
    {
        NativescriptOssFramework.Click();
        return this;
    }

    /// <summary>
    /// Click on Offices Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickOffices1Link() 
    {
        Offices1.Click();
        return this;
    }

    /// <summary>
    /// Click on Offices Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickOffices2Link() 
    {
        Offices2.Click();
        return this;
    }

    /// <summary>
    /// Click on Offices Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickOffices3Link() 
    {
        Offices3.Click();
        return this;
    }

    /// <summary>
    /// Click on Openedge Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickOpenedge1Link() 
    {
        Openedge1.Click();
        return this;
    }

    /// <summary>
    /// Click on Openedge Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickOpenedge2Link() 
    {
        Openedge2.Click();
        return this;
    }

    /// <summary>
    /// Click on Options Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickOptionsLink() 
    {
        Options.Click();
        return this;
    }

    /// <summary>
    /// Click on Partners Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPartners1Link() 
    {
        Partners1.Click();
        return this;
    }

    /// <summary>
    /// Click on Partners Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPartners2Link() 
    {
        Partners2.Click();
        return this;
    }

    /// <summary>
    /// Click on Performance Cookies Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPerformanceCookiesLink() 
    {
        PerformanceCookies.Click();
        return this;
    }

    /// <summary>
    /// Click on Press Coverage Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPressCoverage1Link() 
    {
        PressCoverage1.Click();
        return this;
    }

    /// <summary>
    /// Click on Press Coverage Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPressCoverage2Link() 
    {
        PressCoverage2.Click();
        return this;
    }

    /// <summary>
    /// Click on Press Releases Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPressReleases1Link() 
    {
        PressReleases1.Click();
        return this;
    }

    /// <summary>
    /// Click on Press Releases Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPressReleases2Link() 
    {
        PressReleases2.Click();
        return this;
    }

    /// <summary>
    /// Click on Press Releases Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPressReleases3Link() 
    {
        PressReleases3.Click();
        return this;
    }

    /// <summary>
    /// Click on Pricing Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPricing1Link() 
    {
        Pricing1.Click();
        return this;
    }

    /// <summary>
    /// Click on Pricing Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPricing2Link() 
    {
        Pricing2.Click();
        return this;
    }

    /// <summary>
    /// Click on Privacy Center Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPrivacyCenterLink() 
    {
        PrivacyCenter.Click();
        return this;
    }

    /// <summary>
    /// Click on Privacy Policy Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickPrivacyPolicyLink() 
    {
        PrivacyPolicy.Click();
        return this;
    }

    /// <summary>
    /// Click on Progress Sitefinity Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickProgressSitefinityLink() 
    {
        ProgressSitefinity.Click();
        return this;
    }

    /// <summary>
    /// Click on Release History Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickReleaseHistoryLink() 
    {
        ReleaseHistory.Click();
        return this;
    }

    /// <summary>
    /// Click on Save Settings Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickSaveSettingsLink() 
    {
        SaveSettings.Click();
        return this;
    }

    /// <summary>
    /// Click on Search Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickSearchLink() 
    {
        Search.Click();
        return this;
    }

    /// <summary>
    /// Click on Send Button.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickSendButton() 
    {
        Send.Click();
        return this;
    }

    /// <summary>
    /// Click on 0 Shopping Cart Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickShoppingCartLink0() 
    {
        ShoppingCart0.Click();
        return this;
    }

    /// <summary>
    /// Click on Site Feedback Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickSiteFeedbackLink() 
    {
        SiteFeedback.Click();
        return this;
    }

    /// <summary>
    /// Click on Sitefinity Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickSitefinityLink() 
    {
        Sitefinity.Click();
        return this;
    }

    /// <summary>
    /// Click on Strictly Necessary Cookies Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickStrictlyNecessaryCookiesLink() 
    {
        StrictlyNecessaryCookies.Click();
        return this;
    }

    /// <summary>
    /// Click on Success Stories Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickSuccessStoriesLink() 
    {
        SuccessStories.Click();
        return this;
    }

    /// <summary>
    /// Click on Targeting Cookies Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTargetingCookiesLink() 
    {
        TargetingCookies.Click();
        return this;
    }

    /// <summary>
    /// Click on Technology Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTechnology1Link() 
    {
        Technology1.Click();
        return this;
    }

    /// <summary>
    /// Click on Technology Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTechnology2Link() 
    {
        Technology2.Click();
        return this;
    }

    /// <summary>
    /// Click on Telerik Devcraft Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTelerikDevcraftLink() 
    {
        TelerikDevcraft.Click();
        return this;
    }

    /// <summary>
    /// Click on Telerik Justmock Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTelerikJustmockLink() 
    {
        TelerikJustmock.Click();
        return this;
    }

    /// <summary>
    /// Click on Telerik Report Server Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTelerikReportServerLink() 
    {
        TelerikReportServer.Click();
        return this;
    }

    /// <summary>
    /// Click on Telerik Reporting Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTelerikReportingLink() 
    {
        TelerikReporting.Click();
        return this;
    }

    /// <summary>
    /// Click on Terms Of Use Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTermsOfUseLink() 
    {
        TermsOfUse.Click();
        return this;
    }

    /// <summary>
    /// Click on Test Studio Dev Edition Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTestStudioDevEditionLink() 
    {
        TestStudioDevEdition.Click();
        return this;
    }

    /// <summary>
    /// Click on Test Studio Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTestStudioLink() 
    {
        TestStudio.Click();
        return this;
    }

    /// <summary>
    /// Click on Testimonials Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTestimonialsLink() 
    {
        Testimonials.Click();
        return this;
    }

    /// <summary>
    /// Click on Testing Framework Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTestingFrameworkLink() 
    {
        TestingFramework.Click();
        return this;
    }

    /// <summary>
    /// Click on Trademarks Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickTrademarksLink() 
    {
        Trademarks.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Angular Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForAngularLink() 
    {
        UiForAngular.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Asp.net Ajax Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForAspNetAjaxLink() 
    {
        UiForAspNetAjax.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Asp.net Core Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForAspNetCoreLink() 
    {
        UiForAspNetCore.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Asp.net Mvc Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForAspNetMvcLink() 
    {
        UiForAspNetMvc.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Blazor Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForBlazorLink() 
    {
        UiForBlazor.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Jquery Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForJqueryLink() 
    {
        UiForJquery.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Jsp Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForJspLink() 
    {
        UiForJsp.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Php Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForPhpLink() 
    {
        UiForPhp.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For React Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForReactLink() 
    {
        UiForReact.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Silverlight Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForSilverlightLink() 
    {
        UiForSilverlight.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Uwp Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForUwpLink() 
    {
        UiForUwp.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Vue Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForVueLink() 
    {
        UiForVue.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Winforms Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForWinformsLink() 
    {
        UiForWinforms.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Wpf Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForWpfLink() 
    {
        UiForWpf.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui For Xamarin Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiForXamarinLink() 
    {
        UiForXamarin.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui Tools Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiTools1Link() 
    {
        UiTools1.Click();
        return this;
    }

    /// <summary>
    /// Click on Ui Tools Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUiTools2Link() 
    {
        UiTools2.Click();
        return this;
    }

    /// <summary>
    /// Click on Uk 44 13 4436 0444 Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUk441344360444Link() 
    {
        Uk441344360444.Click();
        return this;
    }

    /// <summary>
    /// Click on Unite Ux Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUniteUxLink() 
    {
        UniteUx.Click();
        return this;
    }

    /// <summary>
    /// Click on Usa 1 888 365 2779 Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickUsa18883652779Link() 
    {
        Usa18883652779.Click();
        return this;
    }

    /// <summary>
    /// Click on Vb.net To C Converter Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickVbNetToCConverterLink() 
    {
        VbNetToCConverter.Click();
        return this;
    }

    /// <summary>
    /// Click on View All Products Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickViewAllProductsLink() 
    {
        ViewAllProducts.Click();
        return this;
    }

    /// <summary>
    /// Click on Virtual Classroom Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickVirtualClassroomLink() 
    {
        VirtualClassroom.Click();
        return this;
    }

    /// <summary>
    /// Click on Vr Dataviz Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickVrDatavizLink() 
    {
        VrDataviz.Click();
        return this;
    }

    /// <summary>
    /// Click on Web Content Management Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickWebContentManagement1Link() 
    {
        WebContentManagement1.Click();
        return this;
    }

    /// <summary>
    /// Click on Web Content Management Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickWebContentManagement2Link() 
    {
        WebContentManagement2.Click();
        return this;
    }

    /// <summary>
    /// Click on Yahoo Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickYahooLink() 
    {
        Yahoo.Click();
        return this;
    }

    /// <summary>
    /// Click on Your Privacy Link.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage ClickYourPrivacyLink() 
    {
        YourPrivacy.Click();
        return this;
    }

    /// <summary>
    /// Fill every fields in the page.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage Fill() 
    {

        SetEmail2EmailField();
        SetFirstNameTextField();
        SetLastNameTextField();
        SetCompany3TextField();
        SetCountryDropDownListField();
        SetPhoneTextField();
        //SetStateprovinceDropDownListField();
        //SetEmail1EmailField();
        //SetPasswordPasswordField();
        //SetKeepMeLoggedInCheckboxField();
        //SetEmailForYourTelerikAccountEmailField();
        //SetContactSupportIfTheProblemPersistsTextField();
        //SetIAgreeToReceiveEmailCommunicationsCheckboxField();
        //SetThankYouForYourContinuedInterest1TextField();
        //SetThankYouForYourContinuedInterest2TextField();
        //SetThankYouForYourContinuedInterest3TextField();
        //SetThankYouForYourContinuedInterest4TextField();
        //SetThankYouForYourContinuedInterest5TextField();
        //SetThankYouForYourContinuedInterest6TextField();
        //SetThankYouForYourContinuedInterest7TextField();
        //SetThankYouForYourContinuedInterest8TextField();
        //SetThankYouForYourContinuedInterest9TextField();
        //SetInactiveCheckboxField();
        //SetTexttospeechFunctionIsLimitedTo2001DropDownListField();
        //SetTexttospeechFunctionIsLimitedTo2002DropDownListField();
        //SetTexttospeechFunctionIsLimitedTo2003DropDownListField();
        //SetTexttospeechFunctionIsLimitedTo2004CheckboxField();
        return this;
    }

    /// <summary>
    /// Fill every fields in the page and submit it to target page.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage FillAndSubmit() 
    {
        Fill();
        return Submit();
    }

    /// <summary>
    /// Set default value to Company Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetCompany3TextField() 
    {
        return SetCompany3TextField(data["COMPANY_3"]);
    }

    /// <summary>
    /// Set value to Company Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetCompany3TextField(string Company3Value)
    {
        Company3.SendKeys(Company3Value);
        return this;
    }

    /// <summary>
    /// Set default value to Contact Support If The Problem Persists Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetContactSupportIfTheProblemPersistsTextField() 
    {
        return SetContactSupportIfTheProblemPersistsTextField(data["CONTACT_SUPPORT_IF_THE_PROBLEM_PERSISTS"]);
    }

    /// <summary>
    /// Set value to Contact Support If The Problem Persists Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetContactSupportIfTheProblemPersistsTextField(string ContactSupportIfTheProblemPersistsValue)
    {
        ContactSupportIfTheProblemPersists.SendKeys(ContactSupportIfTheProblemPersistsValue);
        return this;
    }

    /// <summary>
    /// Set default value to Country Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetCountryDropDownListField() 
    {
        return SetCountryDropDownListField(data["COUNTRY"]);
    }

    /// <summary>
    /// Set value to Country Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetCountryDropDownListField(string CountryValue)
    {
        new SelectElement(Country).SelectByText(CountryValue);
        return this;
    }

    /// <summary>
    /// Set default value to Email Email field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetEmail1EmailField() 
    {
        return SetEmail1EmailField(data["EMAIL_1"]);
    }

    /// <summary>
    /// Set value to Email Email field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetEmail1EmailField(string Email1Value)
    {
        Email1.SendKeys(Email1Value);
        return this;
    }

    /// <summary>
    /// Set default value to Email Email field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetEmail2EmailField() 
    {
        return SetEmail2EmailField(data["EMAIL_2"]);
    }

    /// <summary>
    /// Set value to Email Email field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetEmail2EmailField(string Email2Value)
    {
        Email2.SendKeys(Email2Value);
        return this;
    }

    /// <summary>
    /// Set default value to Email For Your Telerik Account Email field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetEmailForYourTelerikAccountEmailField() 
    {
        return SetEmailForYourTelerikAccountEmailField(data["EMAIL_FOR_YOUR_TELERIK_ACCOUNT"]);
    }

    /// <summary>
    /// Set value to Email For Your Telerik Account Email field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetEmailForYourTelerikAccountEmailField(string EmailForYourTelerikAccountValue)
    {
        EmailForYourTelerikAccount.SendKeys(EmailForYourTelerikAccountValue);
        return this;
    }

    /// <summary>
    /// Set default value to First Name Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetFirstNameTextField() 
    {
        return SetFirstNameTextField(data["FIRST_NAME"]);
    }

    /// <summary>
    /// Set value to First Name Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetFirstNameTextField(string FirstNameValue)
    {
        FirstName.SendKeys(FirstNameValue);
        return this;
    }

    /// <summary>
    /// Set I Agree To Receive Email Communications From Progress Software Or Its Partners Containing Information About Progress Softwares Products. Consent May Be Withdrawn At Any Time. Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetIAgreeToReceiveEmailCommunicationsCheckboxField() 
    {
        if (!IAgreeToReceiveEmailCommunications.Selected) {
            IAgreeToReceiveEmailCommunications.Click();
        }
        return this;
    }

    /// <summary>
    /// Set Inactive Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetInactiveCheckboxField() 
    {
        if (!Inactive.Selected) {
            Inactive.Click();
        }
        return this;
    }

    /// <summary>
    /// Set Keep Me Logged In Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetKeepMeLoggedInCheckboxField() 
    {
        if (!KeepMeLoggedIn.Selected) {
            KeepMeLoggedIn.Click();
        }
        return this;
    }

    /// <summary>
    /// Set default value to Last Name Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetLastNameTextField() 
    {
        return SetLastNameTextField(data["LAST_NAME"]);
    }

    /// <summary>
    /// Set value to Last Name Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetLastNameTextField(string LastNameValue)
    {
        LastName.SendKeys(LastNameValue);
        return this;
    }

    /// <summary>
    /// Set default value to Password Password field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetPasswordPasswordField() 
    {
        return SetPasswordPasswordField(data["PASSWORD"]);
    }

    /// <summary>
    /// Set value to Password Password field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetPasswordPasswordField(string PasswordValue)
    {
        Password.SendKeys(PasswordValue);
        return this;
    }

    /// <summary>
    /// Set default value to Phone Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetPhoneTextField() 
    {
        return SetPhoneTextField(data["PHONE"]);
    }

    /// <summary>
    /// Set value to Phone Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetPhoneTextField(string PhoneValue)
    {
        Phone.SendKeys(PhoneValue);
        return this;
    }

    /// <summary>
    /// Set default value to Stateprovince Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetStateprovinceDropDownListField() 
    {
        return SetStateprovinceDropDownListField(data["STATEPROVINCE"]);
    }

    /// <summary>
    /// Set value to Stateprovince Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetStateprovinceDropDownListField(string StateprovinceValue)
    {
        new SelectElement(Stateprovince).SelectByText(StateprovinceValue);
        return this;
    }

    /// <summary>
    /// Set default value to Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2001DropDownListField() 
    {
        return SetTexttospeechFunctionIsLimitedTo2001DropDownListField(data["TEXTTOSPEECH_FUNCTION_IS_LIMITED_TO_200_1"]);
    }

    /// <summary>
    /// Set value to Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2001DropDownListField(string TexttospeechFunctionIsLimitedTo2001Value)
    {
        if (!TexttospeechFunctionIsLimitedTo2001.Selected) {
            TexttospeechFunctionIsLimitedTo2001.Click();
        }
        return this;
    }

    /// <summary>
    /// Set default value to Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2002DropDownListField() 
    {
        return SetTexttospeechFunctionIsLimitedTo2002DropDownListField(data["TEXTTOSPEECH_FUNCTION_IS_LIMITED_TO_200_2"]);
    }

    /// <summary>
    /// Set value to Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2002DropDownListField(string TexttospeechFunctionIsLimitedTo2002Value)
    {
        new SelectElement(TexttospeechFunctionIsLimitedTo2002).SelectByText(TexttospeechFunctionIsLimitedTo2002Value);
        return this;
    }

    /// <summary>
    /// Set default value to Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2003DropDownListField() 
    {
        return SetTexttospeechFunctionIsLimitedTo2003DropDownListField(data["TEXTTOSPEECH_FUNCTION_IS_LIMITED_TO_200_3"]);
    }

    /// <summary>
    /// Set value to Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2003DropDownListField(string TexttospeechFunctionIsLimitedTo2003Value)
    {
        new SelectElement(TexttospeechFunctionIsLimitedTo2003).SelectByText(TexttospeechFunctionIsLimitedTo2003Value);
        return this;
    }

    /// <summary>
    /// Set Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetTexttospeechFunctionIsLimitedTo2004CheckboxField() 
    {
        if (!TexttospeechFunctionIsLimitedTo2004.Selected) {
            TexttospeechFunctionIsLimitedTo2004.Click();
        }
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest1TextField() 
    {
        return SetThankYouForYourContinuedInterest1TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_1"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest1TextField(string ThankYouForYourContinuedInterest1Value)
    {
        if (!ThankYouForYourContinuedInterest1.Selected) {
            ThankYouForYourContinuedInterest1.Click();
        }
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest2TextField() 
    {
        return SetThankYouForYourContinuedInterest2TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_2"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest2TextField(string ThankYouForYourContinuedInterest2Value)
    {
        ThankYouForYourContinuedInterest2.SendKeys(ThankYouForYourContinuedInterest2Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest3TextField() 
    {
        return SetThankYouForYourContinuedInterest3TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_3"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest3TextField(string ThankYouForYourContinuedInterest3Value)
    {
        ThankYouForYourContinuedInterest3.SendKeys(ThankYouForYourContinuedInterest3Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest4TextField() 
    {
        return SetThankYouForYourContinuedInterest4TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_4"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest4TextField(string ThankYouForYourContinuedInterest4Value)
    {
        ThankYouForYourContinuedInterest4.SendKeys(ThankYouForYourContinuedInterest4Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest5TextField() 
    {
        return SetThankYouForYourContinuedInterest5TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_5"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest5TextField(string ThankYouForYourContinuedInterest5Value)
    {
        ThankYouForYourContinuedInterest5.SendKeys(ThankYouForYourContinuedInterest5Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest6TextField() 
    {
        return SetThankYouForYourContinuedInterest6TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_6"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest6TextField(string ThankYouForYourContinuedInterest6Value)
    {
        ThankYouForYourContinuedInterest6.SendKeys(ThankYouForYourContinuedInterest6Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest7TextField() 
    {
        return SetThankYouForYourContinuedInterest7TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_7"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest7TextField(string ThankYouForYourContinuedInterest7Value)
    {
        ThankYouForYourContinuedInterest7.SendKeys(ThankYouForYourContinuedInterest7Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest8TextField() 
    {
        return SetThankYouForYourContinuedInterest8TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_8"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest8TextField(string ThankYouForYourContinuedInterest8Value)
    {
        ThankYouForYourContinuedInterest8.SendKeys(ThankYouForYourContinuedInterest8Value);
        return this;
    }

    /// <summary>
    /// Set default value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest9TextField() 
    {
        return SetThankYouForYourContinuedInterest9TextField(data["THANK_YOU_FOR_YOUR_CONTINUED_INTEREST_9"]);
    }

    /// <summary>
    /// Set value to Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Text field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage SetThankYouForYourContinuedInterest9TextField(string ThankYouForYourContinuedInterest9Value)
    {
        ThankYouForYourContinuedInterest9.SendKeys(ThankYouForYourContinuedInterest9Value);
        return this;
    }

    /// <summary>
    /// Submit the form to target page.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage Submit() 
    {
        ClickCreateAccountButton();
        return this;
    }

    /// <summary>
    /// Unset default value from Country Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetCountryDropDownListField() 
    {
        return UnsetCountryDropDownListField(data["COUNTRY"]);
    }

    /// <summary>
    /// Unset value from Country Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetCountryDropDownListField(string CountryValue)
    {
        new SelectElement(Country).DeselectByText(CountryValue);
        return this;
    }

    /// <summary>
    /// Unset I Agree To Receive Email Communications From Progress Software Or Its Partners Containing Information About Progress Softwares Products. Consent May Be Withdrawn At Any Time. Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetIAgreeToReceiveEmailCommunicationsCheckboxField() 
    {
        if (IAgreeToReceiveEmailCommunications.Selected) {
            IAgreeToReceiveEmailCommunications.Click();
        }
        return this;
    }

    /// <summary>
    /// Unset Inactive Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetInactiveCheckboxField() 
    {
        if (Inactive.Selected) {
            Inactive.Click();
        }
        return this;
    }

    /// <summary>
    /// Unset Keep Me Logged In Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetKeepMeLoggedInCheckboxField() 
    {
        if (KeepMeLoggedIn.Selected) {
            KeepMeLoggedIn.Click();
        }
        return this;
    }

    /// <summary>
    /// Unset default value from Stateprovince Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetStateprovinceDropDownListField() 
    {
        return UnsetStateprovinceDropDownListField(data["STATEPROVINCE"]);
    }

    /// <summary>
    /// Unset value from Stateprovince Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetStateprovinceDropDownListField(string StateprovinceValue)
    {
        new SelectElement(Stateprovince).DeselectByText(StateprovinceValue);
        return this;
    }

    /// <summary>
    /// Unset default value from Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2001DropDownListField() 
    {
        return UnsetTexttospeechFunctionIsLimitedTo2001DropDownListField(data["TEXTTOSPEECH_FUNCTION_IS_LIMITED_TO_200_1"]);
    }

    /// <summary>
    /// Unset value from Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2001DropDownListField(string TexttospeechFunctionIsLimitedTo2001Value)
    {
        if (TexttospeechFunctionIsLimitedTo2001.Selected) {
            TexttospeechFunctionIsLimitedTo2001.Click();
        }
        return this;
    }

    /// <summary>
    /// Unset default value from Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2002DropDownListField() 
    {
        return UnsetTexttospeechFunctionIsLimitedTo2002DropDownListField(data["TEXTTOSPEECH_FUNCTION_IS_LIMITED_TO_200_2"]);
    }

    /// <summary>
    /// Unset value from Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2002DropDownListField(string TexttospeechFunctionIsLimitedTo2002Value)
    {
        new SelectElement(TexttospeechFunctionIsLimitedTo2002).DeselectByText(TexttospeechFunctionIsLimitedTo2002Value);
        return this;
    }

    /// <summary>
    /// Unset default value from Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2003DropDownListField() 
    {
        return UnsetTexttospeechFunctionIsLimitedTo2003DropDownListField(data["TEXTTOSPEECH_FUNCTION_IS_LIMITED_TO_200_3"]);
    }

    /// <summary>
    /// Unset value from Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Drop Down List field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2003DropDownListField(string TexttospeechFunctionIsLimitedTo2003Value)
    {
        new SelectElement(TexttospeechFunctionIsLimitedTo2003).DeselectByText(TexttospeechFunctionIsLimitedTo2003Value);
        return this;
    }

    /// <summary>
    /// Unset Texttospeech Function Is Limited To 200 Charactersoptions History Feedback Donateclose Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetTexttospeechFunctionIsLimitedTo2004CheckboxField() 
    {
        if (TexttospeechFunctionIsLimitedTo2004.Selected) {
            TexttospeechFunctionIsLimitedTo2004.Click();
        }
        return this;
    }

    /// <summary>
    /// Unset Thank You For Your Continued Interest In Progress. Based On Either Your Previous Activity On Our Websites Or Our Ongoing Relationship We Will Keep You Updated On Our Products Solutions Services Company News And Events. If You Decide That You Want To Be Removed From Our Mailing Lists At Any Time You Can Change Your Contact Preferences By Clicking Here. Checkbox field.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage UnsetThankYouForYourContinuedInterestCheckboxField() 
    {
        //if (ThankYouForYourContinuedInterest.Selected) {
        //    ThankYouForYourContinuedInterest.Click();
        //}
        return this;
    }

    /// <summary>
    /// Verify that the page loaded completely.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage VerifyPageLoaded() 
    {
        new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until<bool>((d) =>
        {
            return d.PageSource.Contains(PageLoadedText);
        });
        return this;
    }

    /// <summary>
    /// Verify that current page URL matches the expected URL.
    /// </summary>
    /// <returns>The RegisterPage class instance.</returns>
    public RegisterPage VerifyPageUrl() 
    {
        new WebDriverWait(driver, TimeSpan.FromSeconds(timeout)).Until<bool>((d) =>
        {
            return d.Url.Contains(PageUrl);
        });
        return this;
    }
}
