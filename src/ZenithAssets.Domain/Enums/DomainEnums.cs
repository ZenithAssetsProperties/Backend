namespace ZenithAssets.Domain.Enums;

public enum UserRole
{
    PublicUser = 1,
    Buyer = 2,
    DiasporaBuyer = 3,
    Agent = 4,
    Admin = 5,
    SuperAdmin = 6
}

public enum PropertyType
{
    Land = 1,
    ResidentialHouse = 2,
    Apartment = 3,
    Commercial = 4,
    Investment = 5
}

public enum PropertyStatus
{
    Draft = 1,
    Available = 2,
    ReservedPending = 3,
    ReservedConfirmed = 4,
    UnderContract = 5,
    Sold = 6,
    Archived = 7
}

public enum DevelopmentStatus
{
    OffPlan = 1,
    UnderConstruction = 2,
    Completed = 3
}

public enum VerificationStatus
{
    Pending = 1,
    ZenithVerified = 2,
    Unverified = 3
}

public enum ReservationStatus
{
    PendingPayment = 1,
    Confirmed = 2,
    Expired = 3,
    Cancelled = 4,
    ConvertedToPurchase = 5
}

public enum TransactionType
{
    ReservationFee = 1,
    OutrightPurchase = 2,
    InstallmentDeposit = 3,
    InstallmentMilestone = 4,
    DocumentationFee = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Abandoned = 4,
    Refunded = 5
}

public enum PaymentChannel
{
    Card = 1,
    BankTransfer = 2,
    DedicatedVirtualAccount = 3,
    Ussd = 4
}

public enum InspectionType
{
    Physical = 1,
    VirtualTour360 = 2,
    LiveVideoCall = 3
}

public enum InspectionStatus
{
    Requested = 1,
    Confirmed = 2,
    Completed = 3,
    Cancelled = 4,
    Rescheduled = 5,
    NoShow = 6
}

public enum DocumentType
{
    SurveyPlan = 1,
    DeedOfAssignment = 2,
    CertificateOfOccupancy = 3,
    GovernorsConsent = 4,
    AllocationLetter = 5,
    ApprovedBuildingPlan = 6,
    TitleDocument = 7,
    Receipt = 8
}

public enum DocumentAccessStatus
{
    PendingReview = 1,
    Approved = 2,
    Rejected = 3,
    Revoked = 4
}

public enum LeadSource
{
    Website = 1,
    WhatsApp = 2,
    Instagram = 3,
    Referral = 4,
    DirectCall = 5
}

public enum LeadStatus
{
    New = 1,
    Contacted = 2,
    InspectionScheduled = 3,
    Negotiation = 4,
    UnderContract = 5,
    ClosedWon = 6,
    ClosedLost = 7
}
