namespace DoAnChuyennNganh.Models
{
    public class QuizSubmissionModel
    {
        public int LectureId { get; set; }
        public string LectureName { get; set; }
        public string Video_url { get; set; }
        public string Document_url { get; set; }
        public string Description { get; set; }
        public bool HasSubmitted { get; set; }
        public List<QuestionResult> QuizResults { get; set; } = new List<QuestionResult>();
    }

    public class QuestionResult
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public List<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();
        public int? SelectedAnswerId { get; set; } // Lựa chọn của người dùng
    }

    public class AnswerOption
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }
    }
    public class QuizSubmissionRequest
    {
        public int LectureId { get; set; }
        public int CourseId { get; set; }
        public List<QuizAnswer> Answers { get; set; }
    }

    public class QuizAnswer
    {
        public int QuestionId { get; set; }
        public int SelectedAnswerId { get; set; }
    }
}
