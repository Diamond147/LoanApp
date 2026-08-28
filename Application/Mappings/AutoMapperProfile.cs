using AutoMapper;
using Domain.DTOs.Emails;
using Domain.DTOs.Payments;
using Domain.DTOs.Users.RequestDto;
using Domain.DTOs.Users.ResponseDto;
using Domain.Entities;


namespace Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // Source -> Destination
            // ForMembers are only applied where property names differ or values need transforming, and
            // simple 1:1 mappings are only applied where property names are identical/aligned.

            // User mappings
            CreateMap<CreateUserProfileDto, UserProfile>();
            CreateMap<UserProfile, UserProfileDto>();
            CreateMap<UserProfile, LoginResponseDto>();

            // Loan mappings
            CreateMap<CreateLoanDto, Loan>();
            CreateMap<Loan, LoanDto>();

            // Map Loan -> LoanHistory (snapshot)
            CreateMap<Loan, LoanHistory>()
                .ForMember(dest => dest.LoanId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.PrincipalBalance, opt => opt.MapFrom(src => src.PrincipalBalance))
                .ForMember(dest => dest.RequestedAmount, opt => opt.MapFrom(src => src.RequestedAmount))
                .ForMember(dest => dest.InterestRate, opt => opt.MapFrom(src => src.InterestRate))
                .ForMember(dest => dest.RequestedDate, opt => opt.MapFrom(src => src.RequestedDate))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.UserProfileId, opt => opt.MapFrom(src => src.UserProfileId));

            // Loan history
            CreateMap<LoanHistory, LoanHistoryDto>();

            // Payments
            CreateMap<Payment, PaymentDto>();
            CreateMap<Payment, PaymentResponseDto>()
            .ForMember(dest => dest.Reference, opt => opt.MapFrom(src => src.PaystackReference));

            // Prequalified loans
            CreateMap<CreatePreQualifiedLoanDto, PreQualifiedLoan>();
            CreateMap<PreQualifiedLoan, PreQualifiedLoanDto>();

            // Email logging
            CreateMap<EmailDto, EmailLog>();

            // Dashboard & aggregate mappings
            CreateMap<UserProfile, LoanDashboardDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.Loans, opt => opt.MapFrom(src => src.Loans))
                .ForMember(dest => dest.LoanHistory, opt => opt.MapFrom(src => src.Loans.SelectMany(l => l.LoanHistories)));

            CreateMap<UserProfile, AllUserDetailsDto>()
                .ForMember(dest => dest.Loans, opt => opt.MapFrom(src => src.Loans))
                .ForMember(dest => dest.LoanHistories, opt => opt.MapFrom(src => src.Loans.SelectMany(l => l.LoanHistories)));
        }
    }
}
