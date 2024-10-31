namespace BrainStormEra.ViewModels
{
    public class ConversationViewModel
    {
        public string ConversationId { get; set; }
        public string UserName { get; set; }
        public DateTime ConversationTime { get; set; }
        public string ConversationContent { get; set; }

        public List<ConversationViewModel> Conversations { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }

        public DateTime Date { get; set; }
        public int Count { get; set; }

    }
}
