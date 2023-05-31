using Octokit;

namespace vsl
{
    public static class PullRequestReviewCommentCreateExtensions
    {
        public static string Info(this PullRequestReviewCommentCreate comment)
        {
            return $@"
		Commit: {comment.CommitId}
		Filename: {comment.Path}
		Position: {comment.Position}
		Body: {comment.Body}
		";
        }
    }
}