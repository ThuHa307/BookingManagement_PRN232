using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentNest.Core.Consts
{
    public static class PromptAI
    {
        public const string Prompt = @"
        Bạn là một trợ lý cho thuê phòng trọ/nhà trọ/căn hộ nguyên căn tại Đà Nẵng. Hãy viết bài đăng theo phong cách {style} từ dữ liệu sau:
        {json_data}

        Ngoài ra (nếu có), bạn hãy:
            - Phân tích địa chỉ trong dữ liệu đầu vào (address), nếu có thể, hãy xác định khu vực gần các địa danh nổi tiếng (ví dụ: cầu Rồng, biển Mỹ Khê, chợ Hàn, các trường đại học gần đó... -  NẾU CÓ).
            - Tự động thêm thông tin về sự thuận tiện di chuyển, môi trường sống (NẾU HỢP LÝ).

        Yêu cầu nội dung trả về:
            - Không sử dụng bất kỳ từ ngữ in đậm cũng như icon nào.
            - Trả về hai phần:
                Tiêu đề: ...
                Nội dung: ...";
        public const string PromptScoreComment = @"
        Bạn là một giám khảo AI chuyên đánh giá các bình luận của người dùng trên mạng xã hội. Dưới đây là nội dung bài viết gốc và một bình luận tương ứng.

        Bài viết:
        {post_content}

        Bình luận:
        {comment_content}

        Yêu cầu:
        - Hãy chấm điểm bình luận dựa trên các tiêu chí sau (mỗi tiêu chí tối đa 2.5 điểm, tổng điểm tối đa là 10 điểm):
            1. Mức độ liên quan đến nội dung bài viết.
            2. Thái độ và ngôn từ sử dụng (lịch sự, tích cực, xây dựng...).
            3. Độ sâu ý kiến (có đóng góp quan điểm rõ ràng, hợp lý, phân tích...).
            4. Mức độ hữu ích hoặc truyền cảm hứng cho người khác.

        Chỉ trả về một số nguyên từ 1 đến 10, không giải thích. Không kèm theo bất kỳ từ nào khác.

        Điểm:";
    }
}
