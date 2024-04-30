//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Reflection;
//using System.ComponentModel;
//using Calyx.PathDAL.Library.Extension;

//namespace Calyx.PathDAL.Library.Extension
//{
//	public static class EnumHelper
//	{
//		public static string GetDescription(Enum en)
//		{
//			Type type = en.GetType();

//			MemberInfo[] memInfo = type.GetMember(en.ToString());

//			if (memInfo != null && memInfo.Length > 0)
//			{
//				object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

//				if (attrs != null && attrs.Length > 0)
//				{
//					return ((DescriptionAttribute)attrs[0]).Description;
//				}
//			}

//			return en.ToString();
//		}
//	}
//}

////public IEnumerable<BusinessRuleMasterInfo> GetBusinessRuleMaster(BusinessRuleName pBusinessRuleName)
////{
////    string businessRuleName = EnumHelper.GetDescription(pBusinessRuleName);



//// Create by Bliss : 3.19.2013
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Reflection;
//using System.ComponentModel;

//namespace Calyx.PathDAL.BusinessRule
//{
//    /// <summary>
//    /// Create date : 3.19.2013 (by bliss)
//    /// Update history : ...
//    /// </summary>
//    public enum BusinessRuleName
//    {
//        [Description("Calyx Approved")] CalyxApproved,
//        [Description("Calyx Cancel")] CalyxCancel,
//        [Description("Calyx Canceled or Withdrawn")] CalyxCanceledorWithdrawn,
//        [Description("Calyx Change Request Approved or Submitted to Underwriting")] CalyxChangeRequestApprovedorSubmittedtoUnderwriting,
//        [Description("Calyx Change Request Processed")] CalyxChangeRequestProcessed,
//        [Description("Calyx Change Requested")] CalyxChangeRequested,
//        [Description("Calyx Changed Circumstance Accepted")] CalyxChangedCircumstanceAccepted,
//        [Description("Calyx Changed Circumstance Requested")] CalyxChangedCircumstanceRequested,
//        [Description("Calyx Changed Circumstance Review Complete")] CalyxChangedCircumstanceReviewComplete,
//        [Description("Calyx Clear to Close")] CalyxCleartoClose,
//        [Description("Calyx Collateral & Package Received")] CalyxCollateralAndPackageReceived,
//        [Description("Calyx Collateral Sent")] CalyxCollateralSent,
//        [Description("Calyx Collateral Shipped")] CalyxCollateralShipped,
//        [Description("Calyx Denial 1008")] CalyxDenial1008,
//        [Description("Calyx Denial FHA")] CalyxDenialFHA,
//        [Description("Calyx Denial Requested")] CalyxDenialRequested,
//        [Description("Calyx Denial VA")] CalyxDenialVA,
//        [Description("Calyx Denied")] CalyxDenied,
//        [Description("Calyx Disclosure Ordered")] CalyxDisclosureOrdered,
//        [Description("Calyx Disclosure Prepared")] CalyxDisclosurePrepared,
//        [Description("Calyx Disclosure Sent")] CalyxDisclosureSent,
//        [Description("Calyx Disclosures Ordered")] CalyxDisclosuresOrdered,
//        [Description("Calyx Discloure Not Accepted")] CalyxDiscloureNotAccepted,
//        [Description("Calyx Doc Prep Complete")] CalyxDocPrepComplete,
//        [Description("Calyx Docs Scheduled")] CalyxDocsScheduled,
//        [Description("Calyx Docs Sent")] CalyxDocsSent,
//        [Description("Calyx Employment Verified")] CalyxEmploymentVerified,
//        [Description("Calyx Final Docs Sent")] CalyxFinalDocsSent,
//        [Description("Calyx Funded")] CalyxFunded,
//        [Description("Calyx Funding Review Complete")] CalyxFundingReviewComplete,
//        [Description("Calyx Funds Requested")] CalyxFundsRequested,
//        [Description("Calyx GFE Redisclosed")] CalyxGFERedisclosed,
//        [Description("Calyx GFE Redisclosed OR Ordered")] CalyxGFERedisclosedOROrdered,
//        [Description("Calyx GFE Redisclosure Ordered")] CalyxGFERedisclosureOrdered,
//        [Description("Calyx GFE Redisclosure Prep")] CalyxGFERedisclosurePrep,
//        [Description("Calyx GFE Redisclosure Prepared")] CalyxGFERedisclosurePrepared,
//        [Description("Calyx GFE Redisclosure Returned")] CalyxGFERedisclosureReturned,
//        [Description("Calyx HDMA Reporting Completed")] CalyxHDMAReportingCompleted,
//        [Description("Calyx Initially Disclosed")] CalyxInitiallyDisclosed,
//        [Description("Calyx Intend To Proceed")] CalyxIntendToProceed,
//        [Description("Calyx Intend To Proceed or Not Accepted")] CalyxIntendToProceedorNotAccepted,
//        [Description("Calyx Investor Cleared")] CalyxInvestorCleared,
//        [Description("Calyx Investor Purchased")] CalyxInvestorPurchased,
//        [Description("Calyx Loan Cleared or Denied")] CalyxLoanClearedorDenied,
//        [Description("Calyx Loan Completed")] CalyxLoanCompleted,
//        [Description("Calyx Loan Shipped")] CalyxLoanShipped,
//        [Description("Calyx Lock Cancellation or Extension Requested")] CalyxLockCancellationorExtensionRequested,
//        [Description("Calyx Lock Cancelled")] CalyxLockCancelled,
//        [Description("Calyx Lock Cancelled or in Review")] CalyxLockCancelledorinReview,
//        [Description("Calyx Lock Expiring")] CalyxLockExpiring,
//        [Description("Calyx Lock Extendable")] CalyxLockExtendable,
//        [Description("Calyx Lock Extended or Expiring")] CalyxLockExtendedorExpiring,
//        [Description("Calyx Lock Extension Requested")] CalyxLockExtensionRequested,
//        [Description("Calyx Lock Request Processed")] CalyxLockRequestProcessed,
//        [Description("Calyx Lock Request Requested")] CalyxLockRequestRequested,
//        [Description("Calyx Lock Review Processed")] CalyxLockReviewProcessed,
//        [Description("Calyx Lock Review Requested")] CalyxLockReviewRequested,
//        [Description("Calyx Locked")] CalyxLocked,
//        [Description("Calyx Locked and Not in Review")] CalyxLockedandNotinReview,
//        [Description("Calyx Locked Not Expired")] CalyxLockedNotExpired,
//        [Description("Calyx MERS Assigned")] CalyxMERSAssigned,
//        [Description("Calyx New Loan")] CalyxNewLoan,
//        [Description("Calyx New Loan or Init Disclosure Not Accepted")] CalyxNewLoanorInitDisclosureNotAccepted,
//        [Description("Calyx New Loan Prospect")] CalyxNewLoanProspect,
//        [Description("Calyx New Lock Requestable")] CalyxNewLockRequestable,
//        [Description("Calyx Not Locked")] CalyxNotLocked,
//        [Description("Calyx Orders AUS")] CalyxOrdersAUS,
//        [Description("Calyx Orders Credit")] CalyxOrdersCredit,
//        [Description("Calyx Package Received")] CalyxPackageReceived,
//        [Description("Calyx Redisclosed")] CalyxRedisclosed,
//        [Description("Calyx Redisclosed GFE Redisclosed")] CalyxRedisclosedGFERedisclosed,
//        [Description("Calyx Redisclosed GFE Redisclosed OR Ordered")] CalyxRedisclosedGFERedisclosedOROrdered,
//        [Description("Calyx Redisclosed GFE Redisclosure Ordered")] CalyxRedisclosedGFERedisclosureOrdered,
//        [Description("Calyx Redisclosed GFE Redisclosure Prep")] CalyxRedisclosedGFERedisclosurePrep,
//        [Description("Calyx Redisclosed GFE Redisclosure Prepared")] CalyxRedisclosedGFERedisclosurePrepared,
//        [Description("Calyx Redisclosed GFE Redisclosure Returned")] CalyxRedisclosedGFERedisclosureReturned,
//        [Description("Calyx Redisclosed Redisclosures Prep")] CalyxRedisclosedRedisclosuresPrep,
//        [Description("Calyx Redisclosed TIL and GFE Redisclosed")] CalyxRedisclosedTILandGFERedisclosed,
//        [Description("Calyx Redisclosed TIL and GFE Redisclosed OR Ordered")] CalyxRedisclosedTILandGFERedisclosedOROrdered,
//        [Description("Calyx Redisclosed TIL and GFE Redisclosure Ordered")] CalyxRedisclosedTILandGFERedisclosureOrdered,
//        [Description("Calyx Redisclosed TIL and GFE Redisclosure Prep")] CalyxRedisclosedTILandGFERedisclosurePrep,
//        [Description("Calyx Redisclosed TIL and GFE Redisclosure Prepared")] CalyxRedisclosedTILandGFERedisclosurePrepared,
//        [Description("Calyx Redisclosed TIL and GFE Redisclosure Returned")] CalyxRedisclosedTILandGFERedisclosureReturned,
//        [Description("Calyx Redisclosed TIL Redisclosed")] CalyxRedisclosedTILRedisclosed,
//        [Description("Calyx Redisclosed TIL Redisclosed or Changed Circumstance Approved")] CalyxRedisclosedTILRedisclosedorChangedCircumstanceApproved,
//        [Description("Calyx Redisclosed TIL Redisclosed OR Ordered")] CalyxRedisclosedTILRedisclosedOROrdered,
//        [Description("Calyx Redisclosed TIL Redisclosed or Ordered or Changed Circumstance Approved")] CalyxRedisclosedTILRedisclosedorOrderedorChangedCircumstanceApproved,
//        [Description("Calyx Redisclosed TIL Redisclosure Ordered")] CalyxRedisclosedTILRedisclosureOrdered,
//        [Description("Calyx Redisclosed TIL Redisclosure Prep")] CalyxRedisclosedTILRedisclosurePrep,
//        [Description("Calyx Redisclosed TIL Redisclosure Prep or Changed Circumstance Approved")] CalyxRedisclosedTILRedisclosurePreporChangedCircumstanceApproved,
//        [Description("Calyx Redisclosed TIL Redisclosure Prepared")] CalyxRedisclosedTILRedisclosurePrepared,
//        [Description("Calyx Redisclosed TIL Redisclosure Returned")] CalyxRedisclosedTILRedisclosureReturned,
//        [Description("Calyx Redisclosures Prep")] CalyxRedisclosuresPrep,
//        [Description("Calyx Redraw")] CalyxRedraw,
//        [Description("Calyx Registered")] CalyxRegistered,
//        [Description("Calyx Return Funds Received")] CalyxReturnFundsReceived,
//        [Description("Calyx Submit 1008")] CalyxSubmit1008,
//        [Description("Calyx Submit FHA")] CalyxSubmitFHA,
//        [Description("Calyx Submit VA")] CalyxSubmitVA,
//        [Description("Calyx Submitted")] CalyxSubmitted,
//        [Description("Calyx Submitted to Underwriting")] CalyxSubmittedtoUnderwriting,
//        [Description("Calyx Submitted to Underwriting or 1st Change Requested")] CalyxSubmittedtoUnderwritingor1stChangeRequested,
//        [Description("Calyx TIL and GFE Redisclosed")] CalyxTILandGFERedisclosed,
//        [Description("Calyx TIL and GFE Redisclosed OR Ordered")] CalyxTILandGFERedisclosedOROrdered,
//        [Description("Calyx TIL and GFE Redisclosure Ordered")] CalyxTILandGFERedisclosureOrdered,
//        [Description("Calyx TIL and GFE Redisclosure Prep")] CalyxTILandGFERedisclosurePrep,
//        [Description("Calyx TIL and GFE Redisclosure Prepared")] CalyxTILandGFERedisclosurePrepared,
//        [Description("Calyx TIL and GFE Redisclosure Returned")] CalyxTILandGFERedisclosureReturned,
//        [Description("Calyx TIL Redisclosed")] CalyxTILRedisclosed,
//        [Description("Calyx TIL Redisclosed OR Ordered")] CalyxTILRedisclosedOROrdered,
//        [Description("Calyx TIL Redisclosed or Ordered or Changed Circumstance Approved")] CalyxTILRedisclosedorOrderedorChangedCircumstanceApproved,
//        [Description("Calyx TIL Redisclosure Ordered")] CalyxTILRedisclosureOrdered,
//        [Description("Calyx TIL Redisclosure Prep")] CalyxTILRedisclosurePrep,
//        [Description("Calyx TIL Redisclosure Prep or Changed Circumstance Approved")] CalyxTILRedisclosurePreporChangedCircumstanceApproved,
//        [Description("Calyx TIL Redisclosure Prepared")] CalyxTILRedisclosurePrepared,
//        [Description("Calyx TIL Redisclosure Prepared or Changed Circumstance Approved")] CalyxTILRedisclosurePreparedorChangedCircumstanceApproved,
//        [Description("Calyx TIL Redisclosure Returned")] CalyxTILRedisclosureReturned,
//        [Description("Calyx Underwrite 1008")] CalyxUnderwrite1008,
//        [Description("Calyx Underwrite FHA")] CalyxUnderwriteFHA,
//        [Description("Calyx Underwrite VA")] CalyxUnderwriteVA,
//        [Description("Calyx Unfund Initiated")] CalyxUnfundInitiated,
//        [Description("Calyx Validate 1008")] CalyxValidate1008,
//        [Description("Calyx Validate FHA")] CalyxValidateFHA,
//        [Description("Calyx Validate VA")] CalyxValidateVA,
//        [Description("Calyx Validated or 1st Change Requested")] CalyxValidatedor1stChangeRequested,
//        [Description("Calyx Validated or Submitted to Underwriting")] CalyxValidatedorSubmittedtoUnderwriting,
//        [Description("Calyx Vendor Tracking")] CalyxVendorTracking,
//        [Description("Calyx Warehouse Paid")] CalyxWarehousePaid,
//        [Description("Calyx Wire Calculated")] CalyxWireCalculated,
//        [Description("Calyx Withdraw")] CalyxWithdraw,
//        [Description("WVHDF Not Locked")] WVHDFNotLocked
//    }
//}
