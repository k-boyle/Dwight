package casino.dwight.bootlegdi;

public class DependencyException extends RuntimeException {
    DependencyException(String message, Exception exception) {
        super(message, exception);
    }
}
