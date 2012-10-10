package ch.cyberduck.core.transfer.copy;

import ch.cyberduck.core.AbstractTestCase;
import ch.cyberduck.core.Host;
import ch.cyberduck.core.NullPath;
import ch.cyberduck.core.NullSession;
import ch.cyberduck.core.Path;
import ch.cyberduck.core.Session;

import org.junit.Test;

import java.util.HashMap;

import static org.junit.Assert.*;

/**
 * @version $Id$
 */
public class CopyTransferFilterTest extends AbstractTestCase {

    @Test
    public void testAcceptDirectoryNew() throws Exception {
        final HashMap<Path, Path> files = new HashMap<Path, Path>();
        final NullPath source = new NullPath("a", Path.DIRECTORY_TYPE);
        files.put(source, new NullPath("a", Path.DIRECTORY_TYPE) {
            @Override
            public boolean exists() {
                return false;
            }
        });
        CopyTransferFilter f = new CopyTransferFilter(files);
        assertTrue(f.accept(source));
    }

    @Test
    public void testAcceptDirectoryExists() throws Exception {
        final HashMap<Path, Path> files = new HashMap<Path, Path>();
        final NullPath source = new NullPath("a", Path.DIRECTORY_TYPE);
        files.put(source, new NullPath("a", Path.DIRECTORY_TYPE) {
            @Override
            public boolean exists() {
                return true;
            }
        });
        CopyTransferFilter f = new CopyTransferFilter(files);
        assertFalse(f.accept(source));
    }

    @Test
    public void testPrepareFile() throws Exception {
        final HashMap<Path, Path> files = new HashMap<Path, Path>();
        final NullPath source = new NullPath("a", Path.FILE_TYPE);
        source.attributes().setSize(1L);
        files.put(source, new NullPath("a", Path.FILE_TYPE));
        CopyTransferFilter f = new CopyTransferFilter(files);
        f.prepare(source);
        assertEquals(2L, source.status().getLength());
    }

    @Test
    public void testPrepareDirectory() throws Exception {
        final HashMap<Path, Path> files = new HashMap<Path, Path>();
        final NullPath source = new NullPath("a", Path.DIRECTORY_TYPE);
        source.attributes().setSize(1L);
        final NullPath target = new NullPath("a", Path.DIRECTORY_TYPE) {

            @Override
            public boolean exists() {
                return false;
            }

            NullSession session = new NullSession(new Host("t"));

            @Override
            public Session getSession() {
                return session;
            }
        };
        files.put(source, target);
        CopyTransferFilter f = new CopyTransferFilter(files);
        f.prepare(source);
        assertEquals(0L, source.status().getLength());
        assertTrue(target.getSession().cache().isCached(target.getReference()));
    }
}
