thisdir = tools

SUBDIRS = extract-source moonlight

include ../build/rules.make

DISTFILES = Makefile

clean-local test-local run-test-local run-test-ondotnet-local all-local install-local uninstall-local:
	@:

dist-local: dist-default

tune:
	$(MAKE) -C moonlight tune