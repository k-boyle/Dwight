package casino.dwight.coc;

public class SerialisationException extends RuntimeException {
    public SerialisationException(String message, Exception ex) {
        super(message, ex);
    }
}
