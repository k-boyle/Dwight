package casino.dwight.coc;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.ObjectReader;

import java.io.IOException;
import java.util.concurrent.ConcurrentHashMap;

public class JsonDeserialiser {
    private final ConcurrentHashMap<Class<?>, Converter<?>> converters;
    private final ObjectMapper mapper;

    public JsonDeserialiser() {
        this.converters = new ConcurrentHashMap<>();
        this.mapper = new ObjectMapper();
    }

    @SuppressWarnings("unchecked")
    public <T> T fromJson(Class<T> clazz, String json) {
        var converter = (Converter<T>) converters.computeIfAbsent(clazz, cl -> new Converter<>(mapper, (Class<T>) cl));
        try {
            return converter.fromJson(json);
        } catch (IOException e) {
            return null;
        }
    }

    private static class Converter<T> {
        private final ObjectReader reader;

        public Converter(ObjectMapper mapper, Class<T> clazz) {
            this.reader = mapper.readerFor(clazz);
        }

        public T fromJson(String json) throws IOException {
            return reader.readValue(json);
        }
    }
}
