using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Data structures for loan application, approval, and management
    /// </summary>

    [System.Serializable]
    public class LoanApplication
    {
        public string ApplicationId;
        public string ApplicantName;
        public DateTime ApplicationDate;
        public float RequestedAmount;
        public LoanType LoanType;
        public LoanPurpose Purpose;
        public int TermMonths;
        public float ProposedInterestRate;
        public LoanApplicationStatus Status;
        public CreditProfile CreditProfile;
        public BusinessPlan BusinessPlan;
        public List<Collateral> ProposedCollateral = new List<Collateral>();
        public string LenderName;
        public DateTime? ReviewDate;
        public string ReviewNotes;
        public LoanDecision Decision;
    }

    [System.Serializable]
    public class LoanContract
    {
        public string LoanId;
        public string ContractNumber;
        public DateTime OriginationDate;
        public DateTime MaturityDate;
        public float PrincipalAmount;
        public float InterestRate;
        public float CurrentBalance;
        public float MonthlyPayment;
        public int PaymentsMade;
        public int TotalPayments;
        public LoanStatus Status;
        public PaymentSchedule PaymentSchedule;
        public List<LoanPayment> PaymentHistory = new List<LoanPayment>();
        public List<Collateral> Collateral = new List<Collateral>();
        public LoanTerms Terms;
        public float TotalInterestPaid;
        public DateTime? LastPaymentDate;
        public int DaysDelinquent;

        public string Purpose;
        public bool IsActive = true;
    }

    [System.Serializable]
    public class LoanPayment
    {
        public string PaymentId;
        public DateTime PaymentDate;
        public DateTime DueDate;
        public float PaymentAmount;
        public float PrincipalAmount;
        public float InterestAmount;
        public float LateFee;
        public PaymentStatus Status;
        public PaymentType PaymentMethodType;
        public string TransactionId;
        public float RemainingBalance;
    }

    [System.Serializable]
    public class LoanTerms
    {
        public float InterestRate;
        public int TermMonths;
        public PaymentFrequency PaymentFrequency;
        public float OriginationFee;
        public float PrepaymentPenalty;
        public bool AllowsPrepayment;
        public float LatePaymentFee;
        public int GracePeriodDays;
        public List<LoanCovenants> Covenants = new List<LoanCovenants>();
        public Dictionary<string, object> SpecialTerms = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class Collateral
    {
        public string CollateralId;
        public string Description;
        public CollateralType Type;
        public float AppraisedValue;
        public float LoanToValueRatio;
        public CollateralStatus Status;
        public DateTime AppraisalDate;
        public string AppraisalCompany;
        public List<string> SupportingDocuments = new List<string>();
    }

    [System.Serializable]
    public class PaymentSchedule
    {
        public string ScheduleId;
        public List<ScheduledPayment> Payments = new List<ScheduledPayment>();
        public PaymentFrequency Frequency;
        public DateTime FirstPaymentDate;
        public DateTime LastPaymentDate;
        public float TotalScheduledPayments;
    }

    [System.Serializable]
    public class ScheduledPayment
    {
        public int PaymentNumber;
        public DateTime DueDate;
        public float PaymentAmount;
        public float PrincipalAmount;
        public float InterestAmount;
        public float RemainingBalance;
        public bool IsPaid;
        public DateTime? ActualPaymentDate;
    }

    [System.Serializable]
    public class LoanCovenants
    {
        public string CovenantId;
        public CovenantType Type;
        public string Description;
        public Dictionary<string, float> Thresholds = new Dictionary<string, float>();
        public bool IsActive;
        public DateTime LastCheckDate;
        public bool IsCompliant;
    }

    // Loan-related enumerations
    public enum LoanType
    {
        Personal,
        Business,
        Equipment,
        RealEstate,
        WorkingCapital,
        Construction,
        Bridge,
        LineOfCredit,
        Term,
        EquipmentFinancing,
        Working_Capital,
        SBA
    }

    public enum LoanPurpose
    {
        EquipmentPurchase,
        FacilityExpansion,
        WorkingCapital,
        PropertyAcquisition,
        RealEstateConstruction,
        Technology,
        Marketing,
        Inventory,
        Refinancing,
        BusinessAcquisition,
        DebtConsolidation,
        Other
    }

    public enum LoanApplicationStatus
    {
        Draft,
        Submitted,
        UnderReview,
        RequiresDocumentation,
        InUnderwriting,
        Approved,
        ConditionallyApproved,
        Declined,
        Withdrawn,
        Expired,
        CounterOfferMade,
        CounterOfferAccepted,
        Funded
    }

    public enum LoanStatus
    {
        Active,
        InDefault,
        PaidInFull,
        PaidOff,
        ChargedOff,
        InForeclosure,
        Deferred,
        Modified,
        Prepaid,
        Transferred
    }

    public enum PaymentStatus
    {
        Scheduled,
        Paid,
        PartiallyPaid,
        Late,
        Missed,
        InDefault,
        Returned
    }

    public enum PaymentFrequency
    {
        Monthly,
        BiWeekly,
        Weekly,
        Quarterly,
        SemiAnnually,
        Annually
    }

    public enum LoanDecision
    {
        Pending,
        Approved,
        ApprovedWithConditions,
        Declined,
        CounterOffer,
        RequiresMoreInfo,
        Withdrawn
    }

    public enum CollateralType
    {
        RealEstate,
        Equipment,
        Vehicle,
        Inventory,
        AccountsReceivable,
        Securities,
        CashDeposit,
        IntellectualProperty,
        Other
    }

    public enum CollateralStatus
    {
        Proposed,
        Accepted,
        UnderAppraisal,
        Appraised,
        Pledged,
        Released,
        Foreclosed
    }

    public enum CovenantType
    {
        Financial,
        Operational,
        Reporting,
        Negative,
        Affirmative
    }

    public enum PaymentType
    {
        Check,
        BankTransfer,
        CreditCard,
        DebitCard,
        Cash,
        MoneyOrder,
        WireTransfer,
        ACH,
        OnlineBanking,
        AutoPay,
        Other
    }
}
