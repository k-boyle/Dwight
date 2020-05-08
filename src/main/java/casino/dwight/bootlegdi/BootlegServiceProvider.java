package casino.dwight.bootlegdi;

import com.google.common.reflect.ClassPath;

import java.io.IOException;
import java.lang.reflect.Constructor;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.function.Supplier;
import java.util.stream.Collectors;

public class BootlegServiceProvider {
    private static final String BASE_PACKAGE = "casino.dwight";

    private final LazyHashMultiMap<Class<?>, Object> services;

    public BootlegServiceProvider() {
        this.services = new LazyHashMultiMap<>();
    }

    public <T> BootlegServiceProvider add(Class<T> clazz) {
        services.put(clazz, () -> construct(clazz));
        return this;
    }

    public <T> BootlegServiceProvider add(Class<? super T> implClazz, Class<T> clazz) {
        services.put(implClazz, () -> construct(clazz));
        return this;
    }

    public <T> BootlegServiceProvider add(T instance) {
        services._put(instance.getClass(), instance);
        return this;
    }

    public <T> BootlegServiceProvider add(Class<T> implClazz, T instance) {
        services._put(implClazz, instance);
        return this;
    }


    @SuppressWarnings({"UnstableApiUsage", "unchecked"})
    public <T> BootlegServiceProvider addAll(Class<T> clazz) {
        try {
            var classes = ClassPath.from(getClass().getClassLoader()).getTopLevelClassesRecursive(BASE_PACKAGE);
            classes.forEach(classInfo -> {
                var cl = classInfo.load();
                if (clazz.isAssignableFrom(cl)) {
                    add(clazz, (Class<T>) cl);
                }
            });
        } catch (IOException ignore) {
        }
        return this;
    }

    @SuppressWarnings("unchecked")
    public <T> T get(Class<T> clazz) {
        return (T) services.get(clazz).get(0).get();
    }

    @SuppressWarnings("unchecked")
    public <T> List<T> getAll(Class<T> clazz) {
        return (List<T>) services.get(clazz).stream().map(Lazy::get).collect(Collectors.toList());
    }

    @SuppressWarnings("unchecked")
    private <T> T construct(Class<T> clazz) {
        var ctor = (Constructor<T>[])clazz.getConstructors();
        for (Constructor<T> constructor : ctor) {
            var parameters = Arrays.asList(constructor.getParameterTypes());
            services.keySet().containsAll(parameters);
            try {
                Object[] args = parameters.stream().map(this::get).toArray(i -> new Object[parameters.size()]);
                return constructor.newInstance(args);
            } catch (Exception e) {
                throw new DependencyException("Could not construct " + clazz.getName(), e);
            }
        }

        throw new DependencyException("Could not find a viable ctor for " + clazz.getName(), null);
    }

    private static class LazyHashMultiMap<KEY, VALUE> extends HashMap<KEY, List<Lazy<VALUE>>> {
        public void put(KEY key, Supplier<VALUE> factory) {
            super.compute(key, (k, list) -> {
                if (list == null) {
                    list = new ArrayList<>();
                }
                list.add(new Lazy<>(factory));
                return list;
            });
        }

        //type erasure op
        public void _put(KEY key, VALUE value) {
            super.compute(key, (k, list) -> {
                if (list == null) {
                    list = new ArrayList<>();
                }
                list.add(new Lazy<>(value));
                return list;
            });
        }
    }

    private static class Lazy<T> {
        private T value;
        private Supplier<T> factory;

        public Lazy(Supplier<T> factory) {
            this.factory = factory;
        }

        public Lazy(T value) {
            this.value = value;
        }

        public T get() {
            return value == null
                ? (value = factory.get())
                : value;
        }
    }
}
