using AutoMapper;
using BirdPlatFormEcommerce.DEntity;
using BirdPlatFormEcommerce.Order.Responses;
using BirdPlatFormEcommerce.Payment.Responses;

namespace BirdPlatFormEcommerce.Helper
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            // Order mappings
            CreateMap<TbOrder, OrderRespponse>()
                .ForMember(dest => dest.Items, options => options.MapFrom(src => src.TbOrderDetails));
               
    
               

            CreateMap<TbOrderDetail, OrderDetailResponse>();

            // Payment mappings
            CreateMap<TbPayment, PaymentResponse>();
            CreateMap<List<TbOrder>, OrderRespponse>();
        }
    }
}
