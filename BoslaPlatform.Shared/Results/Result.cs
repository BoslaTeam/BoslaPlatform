namespace BoslaPlatform.Shared
{
    public  class Result : IResult
    {
        protected readonly List<Error> _errors = [];

        public bool IsSuccess { get; }

        public bool IsError => !IsSuccess;

        public List<Error> Errors => _errors;

        protected Result(bool isSuccess, List<Error>? errors = null)
        {
            IsSuccess = isSuccess;

            if (errors is not null)
            {
                _errors = errors;
            }
        }

        public static Result Success()
            => new(true);

        public static Result Failure(Error error)
            => new(false, [error]);

        public static Result Failure(List<Error> errors)
            => new(false, errors);

        public static implicit operator Result(Error error)
            => Failure(error);

        public static implicit operator Result(List<Error> errors)
            => Failure(errors);
    }

    public sealed class Result<TValue>
        : Result, IResult<TValue>
    {
        private readonly TValue? _value;

        public TValue Value =>
            IsSuccess
                ? _value!
                : throw new InvalidOperationException(
                    "Cannot access value of failed result.");

        private Result(TValue value)
            : base(true)
        {
            _value = value;
        }

        private Result(List<Error> errors)
            : base(false, errors)
        {
        }

        public static Result<TValue> Success(TValue value)
            => new(value);

        public static new Result<TValue> Failure(Error error)
            => new([error]);

        public static new Result<TValue> Failure(List<Error> errors)
            => new(errors);

        public TResult Match<TResult>(
            Func<TValue, TResult> onSuccess,
            Func<IReadOnlyList<Error>, TResult> onFailure)
        {
            return IsSuccess
                ? onSuccess(Value)
                : onFailure(Errors);
        }

        public static implicit operator Result<TValue>(TValue value)
            => Success(value);

        public static implicit operator Result<TValue>(Error error)
            => Failure(error);

        public static implicit operator Result<TValue>(List<Error> errors)
            => Failure(errors);
    }
}
